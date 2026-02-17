using BettingPlatform.Application.Commands.Ticket;
using BettingPlatform.Application.DTOs;
using BettingPlatform.Application.Events;
using BettingPlatform.Application.Interfaces;
using BettingPlatform.Domain.Entities;
using BettingPlatform.Domain.Enums;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;
using TicketEntity = BettingPlatform.Domain.Entities.Ticket;

namespace BettingPlatform.Application.Tests.Ticket;

public class CreateTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepositoryMock;
    private readonly Mock<ISportEventRepository> _sportEventRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly CreateTicketCommandHandler _handler;

    public CreateTicketCommandHandlerTests()
    {
        _ticketRepositoryMock = new Mock<ITicketRepository>();
        _sportEventRepositoryMock = new Mock<ISportEventRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _mediatorMock = new Mock<IMediator>();

        _handler = new CreateTicketCommandHandler(
            _ticketRepositoryMock.Object,
            _sportEventRepositoryMock.Object,
            _userRepositoryMock.Object,
            _mediatorMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesTicket()
    {
        // Arrange
        var expertId = Guid.NewGuid();
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();

        var command = new CreateTicketCommand
        {
            ExpertId = expertId,
            Title = "Weekend Accumulator",
            Description = "High confidence picks",
            Type = TicketType.Multi,
            Stake = 100m,
            Entries = new List<TicketEntryDto>
            {
                new TicketEntryDto
                {
                    SportEventId = eventId1,
                    Market = "Match Result",
                    Selection = "Home",
                    Odds = 2.0m
                },
                new TicketEntryDto
                {
                    SportEventId = eventId2,
                    Market = "Over/Under",
                    Selection = "Over 2.5",
                    Odds = 1.8m
                }
            }
        };

        var expert = new User
        {
            Id = expertId,
            Email = "expert@example.com",
            Role = UserRole.Expert
        };

        var sportEvent1 = new SportEvent
        {
            Id = eventId1,
            ExternalEventId = "ext-1",
            Sport = "Football",
            HomeTeam = "Team A",
            AwayTeam = "Team B",
            StartTimeUtc = DateTime.UtcNow.AddHours(3),
            Status = EventStatus.Scheduled
        };

        var sportEvent2 = new SportEvent
        {
            Id = eventId2,
            ExternalEventId = "ext-2",
            Sport = "Football",
            HomeTeam = "Team C",
            AwayTeam = "Team D",
            StartTimeUtc = DateTime.UtcNow.AddHours(4),
            Status = EventStatus.Scheduled
        };

        TicketEntity? savedTicket = null;

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(expertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expert);

        _sportEventRepositoryMock
            .Setup(r => r.GetByIdAsync(eventId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sportEvent1);

        _sportEventRepositoryMock
            .Setup(r => r.GetByIdAsync(eventId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sportEvent2);

        _ticketRepositoryMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _ticketRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<TicketEntity>(), It.IsAny<CancellationToken>()))
            .Callback<TicketEntity, CancellationToken>((ticket, ct) => savedTicket = ticket)
            .ReturnsAsync((TicketEntity ticket, CancellationToken ct) => ticket);

        _ticketRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _ticketRepositoryMock
            .Setup(r => r.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<TicketCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(command.Title);
        result.Type.Should().Be(command.Type.ToString());
        result.Stake.Should().Be(command.Stake);
        result.TotalOdds.Should().Be(3.6m); // 2.0 * 1.8
        result.PotentialReturn.Should().Be(360m); // 100 * 3.6

        savedTicket.Should().NotBeNull();
        savedTicket!.ExpertId.Should().Be(expertId);
        savedTicket.Title.Should().Be(command.Title);
        savedTicket.Entries.Should().HaveCount(2);
        savedTicket.TotalOdds.Should().Be(3.6m);

        _ticketRepositoryMock.Verify(
            r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _ticketRepositoryMock.Verify(
            r => r.CommitTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<TicketCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NonExpertUser_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreateTicketCommand
        {
            ExpertId = userId,
            Title = "Test Ticket",
            Entries = new List<TicketEntryDto>()
        };

        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            Role = UserRole.User // Not an expert
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"User with ID {userId} is not an expert");

        _ticketRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<TicketEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_EventNotScheduled_ThrowsException()
    {
        // Arrange
        var expertId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var command = new CreateTicketCommand
        {
            ExpertId = expertId,
            Title = "Test Ticket",
            Entries = new List<TicketEntryDto>
            {
                new TicketEntryDto
                {
                    SportEventId = eventId,
                    Market = "Match Result",
                    Selection = "Home",
                    Odds = 2.0m
                }
            }
        };

        var expert = new User
        {
            Id = expertId,
            Email = "expert@example.com",
            Role = UserRole.Expert
        };

        var sportEvent = new SportEvent
        {
            Id = eventId,
            Status = EventStatus.Live // Already started
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(expertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expert);

        _sportEventRepositoryMock
            .Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sportEvent);

        _ticketRepositoryMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*event {eventId}*status Live*All events must be scheduled*");

        _ticketRepositoryMock.Verify(
            r => r.CommitTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}


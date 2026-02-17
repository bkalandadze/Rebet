using BettingPlatform.Application.Commands.Position;
using BettingPlatform.Application.DTOs;
using BettingPlatform.Application.Events;
using BettingPlatform.Application.Interfaces;
using BettingPlatform.Domain.Entities;
using BettingPlatform.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Moq;
using Xunit;
using PositionEntity = BettingPlatform.Domain.Entities.Position;

namespace BettingPlatform.Application.Tests;

public class CreatePositionCommandHandlerTests
{
    private readonly Mock<ISportEventRepository> _sportEventRepositoryMock;
    private readonly Mock<IPositionRepository> _positionRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly CreatePositionCommandHandler _handler;

    public CreatePositionCommandHandlerTests()
    {
        _sportEventRepositoryMock = new Mock<ISportEventRepository>();
        _positionRepositoryMock = new Mock<IPositionRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _mediatorMock = new Mock<IMediator>();

        _handler = new CreatePositionCommandHandler(
            _sportEventRepositoryMock.Object,
            _positionRepositoryMock.Object,
            _userRepositoryMock.Object,
            _mediatorMock.Object);
    }

    [Fact]
    public async Task Test_ValidCommand_CreatesPosition()
    {
        // Arrange
        var sportEventId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var command = new CreatePositionCommand
        {
            SportEventId = sportEventId,
            Market = "Match Result",
            Selection = "Home",
            Odds = 2.50m,
            Analysis = "Strong home team advantage",
            CreatorId = creatorId
        };

        var sportEvent = new SportEvent
        {
            Id = sportEventId,
            ExternalEventId = "ext-123",
            Sport = "Football",
            League = "Premier League",
            HomeTeam = "Team A",
            AwayTeam = "Team B",
            StartTimeUtc = DateTime.UtcNow.AddHours(2), // 2 hours in future
            Status = EventStatus.Scheduled
        };

        var creator = new User
        {
            Id = creatorId,
            Email = "user@example.com",
            Role = UserRole.User
        };

        PositionEntity? savedPosition = null;
        _sportEventRepositoryMock
            .Setup(r => r.GetByIdAsync(sportEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sportEvent);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(creatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creator);

        _positionRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<PositionEntity>(), It.IsAny<CancellationToken>()))
            .Callback<PositionEntity, CancellationToken>((pos, ct) => savedPosition = pos)
            .ReturnsAsync((PositionEntity pos, CancellationToken ct) => pos);

        _positionRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<PositionCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SportEventId.Should().Be(sportEventId);
        result.CreatorId.Should().Be(creatorId);
        result.Market.Should().Be(command.Market);
        result.Selection.Should().Be(command.Selection);
        result.Odds.Should().Be(command.Odds);
        result.Analysis.Should().Be(command.Analysis);
        result.Status.Should().Be(PositionStatus.Pending.ToString());
        result.CreatorType.Should().Be(UserRole.User.ToString());

        savedPosition.Should().NotBeNull();
        savedPosition!.CreatorId.Should().Be(creatorId);
        savedPosition.CreatorType.Should().Be(UserRole.User);
        savedPosition.SportEventId.Should().Be(sportEventId);
        savedPosition.Market.Should().Be(command.Market);
        savedPosition.Selection.Should().Be(command.Selection);
        savedPosition.Odds.Should().Be(command.Odds);
        savedPosition.Status.Should().Be(PositionStatus.Pending);

        _positionRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<PositionEntity>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _positionRepositoryMock.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(
            m => m.Publish(It.Is<PositionCreatedEvent>(e =>
                e.PositionId == savedPosition.Id &&
                e.CreatorId == creatorId &&
                e.SportEventId == sportEventId &&
                e.Market == command.Market &&
                e.Selection == command.Selection &&
                e.Odds == command.Odds),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Test_InvalidOdds_ThrowsValidationException()
    {
        // Arrange
        var command = new CreatePositionCommand
        {
            SportEventId = Guid.NewGuid(),
            Market = "Match Result",
            Selection = "Home",
            Odds = 0.50m, // Invalid: less than 1.01
            CreatorId = Guid.NewGuid()
        };

        var validator = new CreatePositionCommandValidator();

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreatePositionCommand.Odds) &&
            e.ErrorMessage.Contains("at least 1.01"));
    }

    [Fact]
    public async Task Test_NonExistentEvent_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentEventId = Guid.NewGuid();
        var command = new CreatePositionCommand
        {
            SportEventId = nonExistentEventId,
            Market = "Match Result",
            Selection = "Home",
            Odds = 2.50m,
            CreatorId = Guid.NewGuid()
        };

        _sportEventRepositoryMock
            .Setup(r => r.GetByIdAsync(nonExistentEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SportEvent?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Sport event with ID {nonExistentEventId} does not exist");

        _positionRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<PositionEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Test_StartedEvent_ThrowsBusinessException()
    {
        // Arrange
        var sportEventId = Guid.NewGuid();
        var command = new CreatePositionCommand
        {
            SportEventId = sportEventId,
            Market = "Match Result",
            Selection = "Home",
            Odds = 2.50m,
            CreatorId = Guid.NewGuid()
        };

        var sportEvent = new SportEvent
        {
            Id = sportEventId,
            ExternalEventId = "ext-123",
            Sport = "Football",
            League = "Premier League",
            HomeTeam = "Team A",
            AwayTeam = "Team B",
            StartTimeUtc = DateTime.UtcNow.AddHours(2),
            Status = EventStatus.Live // Event is already live
        };

        _sportEventRepositoryMock
            .Setup(r => r.GetByIdAsync(sportEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sportEvent);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Cannot create position for event with status {EventStatus.Live}. Event must be scheduled.");

        _positionRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<PositionEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Test_EventStartingWithinOneHour_ThrowsBusinessException()
    {
        // Arrange
        var sportEventId = Guid.NewGuid();
        var command = new CreatePositionCommand
        {
            SportEventId = sportEventId,
            Market = "Match Result",
            Selection = "Home",
            Odds = 2.50m,
            CreatorId = Guid.NewGuid()
        };

        var sportEvent = new SportEvent
        {
            Id = sportEventId,
            ExternalEventId = "ext-123",
            Sport = "Football",
            League = "Premier League",
            HomeTeam = "Team A",
            AwayTeam = "Team B",
            StartTimeUtc = DateTime.UtcNow.AddMinutes(30), // Less than 1 hour
            Status = EventStatus.Scheduled
        };

        _sportEventRepositoryMock
            .Setup(r => r.GetByIdAsync(sportEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sportEvent);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot create position for event starting within 1 hour");

        _positionRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<PositionEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Test_PositionCreated_PublishesEvent()
    {
        // Arrange
        var sportEventId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var command = new CreatePositionCommand
        {
            SportEventId = sportEventId,
            Market = "Over/Under",
            Selection = "Over 2.5",
            Odds = 1.85m,
            Analysis = "High scoring teams",
            CreatorId = creatorId
        };

        var sportEvent = new SportEvent
        {
            Id = sportEventId,
            ExternalEventId = "ext-456",
            Sport = "Football",
            League = "La Liga",
            HomeTeam = "Team C",
            AwayTeam = "Team D",
            StartTimeUtc = DateTime.UtcNow.AddHours(3),
            Status = EventStatus.Scheduled
        };

        var creator = new User
        {
            Id = creatorId,
            Email = "expert@example.com",
            Role = UserRole.Expert
        };

        PositionEntity? savedPosition = null;
        _sportEventRepositoryMock
            .Setup(r => r.GetByIdAsync(sportEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sportEvent);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(creatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creator);

        _positionRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<PositionEntity>(), It.IsAny<CancellationToken>()))
            .Callback<PositionEntity, CancellationToken>((pos, ct) => savedPosition = pos)
            .ReturnsAsync((PositionEntity pos, CancellationToken ct) => pos);

        _positionRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        PositionCreatedEvent? publishedEvent = null;
        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<PositionCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<INotification, CancellationToken>((evt, ct) => publishedEvent = evt as PositionCreatedEvent)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.PositionId.Should().Be(savedPosition!.Id);
        publishedEvent.CreatorId.Should().Be(creatorId);
        publishedEvent.CreatorType.Should().Be(UserRole.Expert.ToString());
        publishedEvent.SportEventId.Should().Be(sportEventId);
        publishedEvent.Market.Should().Be(command.Market);
        publishedEvent.Selection.Should().Be(command.Selection);
        publishedEvent.Odds.Should().Be(command.Odds);
        publishedEvent.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<PositionCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Test_NonExistentUser_ThrowsNotFoundException()
    {
        // Arrange
        var sportEventId = Guid.NewGuid();
        var nonExistentUserId = Guid.NewGuid();
        var command = new CreatePositionCommand
        {
            SportEventId = sportEventId,
            Market = "Match Result",
            Selection = "Home",
            Odds = 2.50m,
            CreatorId = nonExistentUserId
        };

        var sportEvent = new SportEvent
        {
            Id = sportEventId,
            ExternalEventId = "ext-123",
            Sport = "Football",
            League = "Premier League",
            HomeTeam = "Team A",
            AwayTeam = "Team B",
            StartTimeUtc = DateTime.UtcNow.AddHours(2),
            Status = EventStatus.Scheduled
        };

        _sportEventRepositoryMock
            .Setup(r => r.GetByIdAsync(sportEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sportEvent);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(nonExistentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"User with ID {nonExistentUserId} does not exist");

        _positionRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<PositionEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}


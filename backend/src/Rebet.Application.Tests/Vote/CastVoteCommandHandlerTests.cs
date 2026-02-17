using BettingPlatform.Application.Commands.Vote;
using BettingPlatform.Application.DTOs;
using BettingPlatform.Application.Events;
using BettingPlatform.Application.Interfaces;
using BettingPlatform.Domain.Entities;
using BettingPlatform.Domain.Enums;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;
using PositionEntity = BettingPlatform.Domain.Entities.Position;
using TicketEntity = BettingPlatform.Domain.Entities.Ticket;
using ExpertEntity = BettingPlatform.Domain.Entities.Expert;
using VoteEntity = BettingPlatform.Domain.Entities.Vote;

namespace BettingPlatform.Application.Tests.Vote;

public class CastVoteCommandHandlerTests
{
    private readonly Mock<IVoteRepository> _voteRepositoryMock;
    private readonly Mock<IPositionRepository> _positionRepositoryMock;
    private readonly Mock<ITicketRepository> _ticketRepositoryMock;
    private readonly Mock<IExpertRepository> _expertRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly CastVoteCommandHandler _handler;

    public CastVoteCommandHandlerTests()
    {
        _voteRepositoryMock = new Mock<IVoteRepository>();
        _positionRepositoryMock = new Mock<IPositionRepository>();
        _ticketRepositoryMock = new Mock<ITicketRepository>();
        _expertRepositoryMock = new Mock<IExpertRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _mediatorMock = new Mock<IMediator>();

        _handler = new CastVoteCommandHandler(
            _voteRepositoryMock.Object,
            _positionRepositoryMock.Object,
            _ticketRepositoryMock.Object,
            _expertRepositoryMock.Object,
            _userRepositoryMock.Object,
            _mediatorMock.Object);
    }

    [Fact]
    public async Task Handle_VoteForPosition_NewUpvote_CreatesVote()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new CastVoteCommand
        {
            UserId = userId,
            VoteableType = 1, // Position
            VoteableId = positionId,
            VoteType = 1 // Upvote
        };

        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            Role = UserRole.User
        };

        var position = new PositionEntity
        {
            Id = positionId,
            CreatorId = Guid.NewGuid(),
            SportEventId = Guid.NewGuid(),
            Market = "Match Result",
            Selection = "Home",
            Odds = 2.0m,
            UpvoteCount = 5,
            DownvoteCount = 2,
            VoterCount = 7,
            Status = PositionStatus.Pending
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _voteRepositoryMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _voteRepositoryMock
            .Setup(r => r.GetVoteAsync(userId, VoteableType.Position, positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteEntity?)null);

        _positionRepositoryMock
            .Setup(r => r.GetByIdAsync(positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(position);

        _voteRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<VoteEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteEntity vote, CancellationToken ct) => vote);

        _voteRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _voteRepositoryMock
            .Setup(r => r.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _positionRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<VoteCastEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.VoteableId.Should().Be(positionId);
        result.VoteableType.Should().Be("Position");
        result.VoteType.Should().Be("upvote");
        result.UpvoteCount.Should().Be(5);
        result.DownvoteCount.Should().Be(2);

        _voteRepositoryMock.Verify(
            r => r.AddAsync(It.Is<VoteEntity>(v => 
                v.UserId == userId && 
                v.VoteableType == VoteableType.Position &&
                v.Type == VoteType.Upvote),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(
            m => m.Publish(It.Is<VoteCastEvent>(e => 
                e.VoteableId == positionId &&
                e.VoteableType == VoteableType.Position &&
                e.VoteType == VoteType.Upvote),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_VoteForTicket_NewUpvote_CreatesVote()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new CastVoteCommand
        {
            UserId = userId,
            VoteableType = 2, // Ticket
            VoteableId = ticketId,
            VoteType = 1 // Upvote
        };

        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            Role = UserRole.User
        };

        var ticket = new TicketEntity
        {
            Id = ticketId,
            ExpertId = Guid.NewGuid(),
            Title = "Test Ticket",
            TotalOdds = 3.0m,
            Stake = 100m,
            UpvoteCount = 10,
            DownvoteCount = 3
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _voteRepositoryMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _voteRepositoryMock
            .Setup(r => r.GetVoteAsync(userId, VoteableType.Ticket, ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteEntity?)null);

        _ticketRepositoryMock
            .Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        _voteRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<VoteEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteEntity vote, CancellationToken ct) => vote);

        _voteRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _voteRepositoryMock
            .Setup(r => r.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<VoteCastEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.VoteableId.Should().Be(ticketId);
        result.VoteableType.Should().Be("Ticket");
        result.VoteType.Should().Be("upvote");
        result.UpvoteCount.Should().Be(10);
        result.DownvoteCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_VoteForExpert_NewUpvote_CreatesVote()
    {
        // Arrange
        var expertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new CastVoteCommand
        {
            UserId = userId,
            VoteableType = 3, // Expert
            VoteableId = expertId,
            VoteType = 1 // Upvote
        };

        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            Role = UserRole.User
        };

        var expert = new ExpertEntity
        {
            Id = expertId,
            UserId = Guid.NewGuid(),
            DisplayName = "Test Expert",
            UpvoteCount = 50,
            DownvoteCount = 5
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _voteRepositoryMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _voteRepositoryMock
            .Setup(r => r.GetVoteAsync(userId, VoteableType.Expert, expertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteEntity?)null);

        _expertRepositoryMock
            .Setup(r => r.GetByIdAsync(expertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expert);

        _voteRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<VoteEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteEntity vote, CancellationToken ct) => vote);

        _voteRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _voteRepositoryMock
            .Setup(r => r.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<VoteCastEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.VoteableId.Should().Be(expertId);
        result.VoteableType.Should().Be("Expert");
        result.VoteType.Should().Be("upvote");
        result.UpvoteCount.Should().Be(50);
        result.DownvoteCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_InvalidVoteableType_ThrowsException()
    {
        // Arrange
        var command = new CastVoteCommand
        {
            UserId = Guid.NewGuid(),
            VoteableType = 99, // Invalid
            VoteableId = Guid.NewGuid(),
            VoteType = 1
        };

        var user = new User
        {
            Id = command.UserId,
            Email = "user@example.com",
            Role = UserRole.User
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _voteRepositoryMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid VoteableType*");
    }

    [Fact]
    public async Task Handle_InvalidVoteType_ThrowsException()
    {
        // Arrange
        var command = new CastVoteCommand
        {
            UserId = Guid.NewGuid(),
            VoteableType = 1, // Position
            VoteableId = Guid.NewGuid(),
            VoteType = 99 // Invalid
        };

        var user = new User
        {
            Id = command.UserId,
            Email = "user@example.com",
            Role = UserRole.User
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _voteRepositoryMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid VoteType*");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var command = new CastVoteCommand
        {
            UserId = nonExistentUserId,
            VoteableType = 1,
            VoteableId = Guid.NewGuid(),
            VoteType = 1
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(nonExistentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"User with ID {nonExistentUserId} not found");
    }

    [Fact]
    public async Task Handle_NonExistentPosition_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var nonExistentPositionId = Guid.NewGuid();
        var command = new CastVoteCommand
        {
            UserId = userId,
            VoteableType = 1, // Position
            VoteableId = nonExistentPositionId,
            VoteType = 1
        };

        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            Role = UserRole.User
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _voteRepositoryMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _voteRepositoryMock
            .Setup(r => r.GetVoteAsync(userId, VoteableType.Position, nonExistentPositionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteEntity?)null);

        _positionRepositoryMock
            .Setup(r => r.GetByIdAsync(nonExistentPositionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PositionEntity?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Position with ID {nonExistentPositionId} not found");

        _voteRepositoryMock.Verify(
            r => r.RollbackTransactionAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingVoteSameType_RemovesVote()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new CastVoteCommand
        {
            UserId = userId,
            VoteableType = 1, // Position
            VoteableId = positionId,
            VoteType = 1 // Upvote
        };

        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            Role = UserRole.User
        };

        var position = new PositionEntity
        {
            Id = positionId,
            CreatorId = Guid.NewGuid(),
            SportEventId = Guid.NewGuid(),
            Market = "Match Result",
            Selection = "Home",
            Odds = 2.0m,
            UpvoteCount = 5,
            DownvoteCount = 2,
            VoterCount = 7,
            Status = PositionStatus.Pending
        };

        var existingVote = new VoteEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VoteableType = VoteableType.Position,
            VoteableId = positionId,
            Type = VoteType.Upvote
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _voteRepositoryMock
            .Setup(r => r.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _voteRepositoryMock
            .Setup(r => r.GetVoteAsync(userId, VoteableType.Position, positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVote);

        _positionRepositoryMock
            .Setup(r => r.GetByIdAsync(positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(position);

        _voteRepositoryMock
            .Setup(r => r.RemoveAsync(existingVote, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _voteRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _voteRepositoryMock
            .Setup(r => r.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _positionRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<VoteCastEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.VoteType.Should().BeNull(); // Vote removed

        _voteRepositoryMock.Verify(
            r => r.RemoveAsync(existingVote, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}


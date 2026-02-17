using BettingPlatform.Application.Commands.Position;
using BettingPlatform.Application.DTOs;
using BettingPlatform.Application.Interfaces;
using BettingPlatform.Domain.Entities;
using BettingPlatform.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;
using PositionEntity = BettingPlatform.Domain.Entities.Position;
using VoteEntity = BettingPlatform.Domain.Entities.Vote;

namespace BettingPlatform.Application.Tests.Position;

public class VotePositionCommandHandlerTests
{
    private readonly Mock<IPositionRepository> _positionRepositoryMock;
    private readonly Mock<IVoteRepository> _voteRepositoryMock;
    private readonly VotePositionCommandHandler _handler;

    public VotePositionCommandHandlerTests()
    {
        _positionRepositoryMock = new Mock<IPositionRepository>();
        _voteRepositoryMock = new Mock<IVoteRepository>();

        _handler = new VotePositionCommandHandler(
            _positionRepositoryMock.Object,
            _voteRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_NewUpvote_IncrementsUpvoteCount()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new VotePositionCommand
        {
            PositionId = positionId,
            UserId = userId,
            VoteType = 1 // Upvote
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

        _positionRepositoryMock
            .Setup(r => r.GetByIdAsync(positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(position);

        _voteRepositoryMock
            .Setup(r => r.GetVoteAsync(userId, VoteableType.Position, positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteEntity?)null);

        _voteRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<VoteEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteEntity vote, CancellationToken ct) => vote);

        _positionRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _voteRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PositionId.Should().Be(positionId);
        result.VoteType.Should().Be("upvote");
        result.UpvoteCount.Should().Be(6); // 5 + 1
        result.DownvoteCount.Should().Be(2);
        result.VoterCount.Should().Be(8); // 7 + 1

        position.UpvoteCount.Should().Be(6);
        position.VoterCount.Should().Be(8);

        _voteRepositoryMock.Verify(
            r => r.AddAsync(It.Is<VoteEntity>(v => 
                v.UserId == userId && 
                v.VoteableType == VoteableType.Position && 
                v.VoteableId == positionId &&
                v.Type == VoteType.Upvote),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NewDownvote_IncrementsDownvoteCount()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new VotePositionCommand
        {
            PositionId = positionId,
            UserId = userId,
            VoteType = 2 // Downvote
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

        _positionRepositoryMock
            .Setup(r => r.GetByIdAsync(positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(position);

        _voteRepositoryMock
            .Setup(r => r.GetVoteAsync(userId, VoteableType.Position, positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteEntity?)null);

        _voteRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<VoteEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteEntity vote, CancellationToken ct) => vote);

        _positionRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _voteRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.VoteType.Should().Be("downvote");
        result.UpvoteCount.Should().Be(5);
        result.DownvoteCount.Should().Be(3); // 2 + 1
        result.VoterCount.Should().Be(8); // 7 + 1
    }

    [Fact]
    public async Task Handle_SameVoteType_TogglesOff()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new VotePositionCommand
        {
            PositionId = positionId,
            UserId = userId,
            VoteType = 1 // Upvote
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

        _positionRepositoryMock
            .Setup(r => r.GetByIdAsync(positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(position);

        _voteRepositoryMock
            .Setup(r => r.GetVoteAsync(userId, VoteableType.Position, positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVote);

        _voteRepositoryMock
            .Setup(r => r.RemoveAsync(existingVote, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _positionRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _voteRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.VoteType.Should().BeNull(); // Vote removed
        result.UpvoteCount.Should().Be(4); // 5 - 1
        result.DownvoteCount.Should().Be(2);
        result.VoterCount.Should().Be(6); // 7 - 1

        _voteRepositoryMock.Verify(
            r => r.RemoveAsync(existingVote, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DifferentVoteType_ChangesVote()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new VotePositionCommand
        {
            PositionId = positionId,
            UserId = userId,
            VoteType = 2 // Downvote (changing from upvote)
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

        _positionRepositoryMock
            .Setup(r => r.GetByIdAsync(positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(position);

        _voteRepositoryMock
            .Setup(r => r.GetVoteAsync(userId, VoteableType.Position, positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVote);

        _voteRepositoryMock
            .Setup(r => r.UpdateAsync(existingVote, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _positionRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _voteRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.VoteType.Should().Be("downvote");
        result.UpvoteCount.Should().Be(4); // 5 - 1
        result.DownvoteCount.Should().Be(3); // 2 + 1
        result.VoterCount.Should().Be(7); // Unchanged

        existingVote.Type.Should().Be(VoteType.Downvote);

        _voteRepositoryMock.Verify(
            r => r.UpdateAsync(existingVote, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidVoteType_ThrowsException()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var command = new VotePositionCommand
        {
            PositionId = positionId,
            UserId = Guid.NewGuid(),
            VoteType = 99 // Invalid
        };

        var position = new PositionEntity
        {
            Id = positionId,
            CreatorId = Guid.NewGuid(),
            SportEventId = Guid.NewGuid(),
            Market = "Match Result",
            Selection = "Home",
            Odds = 2.0m,
            Status = PositionStatus.Pending
        };

        _positionRepositoryMock
            .Setup(r => r.GetByIdAsync(positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(position);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("VoteType must be 1 (Upvote) or 2 (Downvote)");
    }

    [Fact]
    public async Task Handle_NonExistentPosition_ThrowsException()
    {
        // Arrange
        var nonExistentPositionId = Guid.NewGuid();
        var command = new VotePositionCommand
        {
            PositionId = nonExistentPositionId,
            UserId = Guid.NewGuid(),
            VoteType = 1
        };

        _positionRepositoryMock
            .Setup(r => r.GetByIdAsync(nonExistentPositionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PositionEntity?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Position with ID {nonExistentPositionId} not found");
    }

    [Fact]
    public async Task Handle_RecalculatesPredictionPercentage()
    {
        // Arrange
        var positionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new VotePositionCommand
        {
            PositionId = positionId,
            UserId = userId,
            VoteType = 1
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

        _positionRepositoryMock
            .Setup(r => r.GetByIdAsync(positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(position);

        _voteRepositoryMock
            .Setup(r => r.GetVoteAsync(userId, VoteableType.Position, positionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteEntity?)null);

        _voteRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<VoteEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoteEntity vote, CancellationToken ct) => vote);

        _positionRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _voteRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.PredictionPercentage.Should().BeGreaterThanOrEqualTo(0);
        result.PredictionPercentage.Should().BeLessThanOrEqualTo(100);
    }
}


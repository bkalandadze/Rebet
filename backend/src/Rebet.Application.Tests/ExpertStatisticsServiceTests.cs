using BettingPlatform.Application.DTOs;
using BettingPlatform.Application.Events;
using BettingPlatform.Application.Interfaces;
using BettingPlatform.Domain.Entities;
using BettingPlatform.Domain.Enums;
using BettingPlatform.Infrastructure.Persistence;
using BettingPlatform.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using PositionEntity = BettingPlatform.Domain.Entities.Position;
using ExpertEntity = BettingPlatform.Domain.Entities.Expert;

namespace BettingPlatform.Application.Tests;

public class ExpertStatisticsServiceTests
{
    private readonly Mock<IPositionRepository> _positionRepositoryMock;
    private readonly Mock<IExpertRepository> _expertRepositoryMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ApplicationDbContext _context;
    private readonly ExpertStatisticsService _service;

    public ExpertStatisticsServiceTests()
    {
        _positionRepositoryMock = new Mock<IPositionRepository>();
        _expertRepositoryMock = new Mock<IExpertRepository>();
        _mediatorMock = new Mock<IMediator>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _service = new ExpertStatisticsService(
            _context,
            _positionRepositoryMock.Object,
            _expertRepositoryMock.Object,
            _mediatorMock.Object);

        // Setup default mocks for leaderboard and mediator
        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        var emptyLeaderboard = new PagedResult<ExpertListDto>
        {
            Data = new List<ExpertListDto>(),
            Pagination = new PagedResult<ExpertListDto>.PaginationMetadata
            {
                Page = 1,
                PageSize = 10,
                TotalItems = 0,
                TotalPages = 0
            }
        };

        _expertRepositoryMock
            .Setup(r => r.GetLeaderboardAsync(
                It.IsAny<string>(), 
                It.IsAny<string?>(), 
                It.IsAny<decimal?>(), 
                It.IsAny<int?>(), 
                It.IsAny<int>(), 
                It.IsAny<int>(), 
                It.IsAny<Guid?>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyLeaderboard);

        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<ExpertStatisticsRecalculatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task RecalculateStatisticsAsync_WithNoPositions_ShouldSetZeroStatistics()
    {
        // Arrange
        var expertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expert = new ExpertEntity
        {
            Id = expertId,
            UserId = userId,
            DisplayName = "Test Expert",
            Status = ExpertStatus.Active,
            Tier = ExpertTier.Bronze
        };

        _context.Experts.Add(expert);
        await _context.SaveChangesAsync();

        _expertRepositoryMock
            .Setup(r => r.GetByIdAsync(expertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expert);

        _positionRepositoryMock
            .Setup(r => r.GetByCreatorIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<PositionEntity>());

        // Act
        await _service.RecalculateStatisticsAsync(expertId);

        // Assert
        var statistics = await _context.ExpertStatistics
            .FirstOrDefaultAsync(s => s.ExpertId == expertId);

        Assert.NotNull(statistics);
        Assert.Equal(0, statistics.TotalPositions);
        Assert.Equal(0, statistics.WonPositions);
        Assert.Equal(0, statistics.LostPositions);
        Assert.Equal(0, statistics.VoidPositions);
        Assert.Equal(0, statistics.PendingPositions);
        Assert.Equal(0.00m, statistics.WinRate);
        Assert.Equal(0.00m, statistics.AverageOdds);
        Assert.Equal(0, statistics.CurrentStreak);
        Assert.Equal(0, statistics.LongestWinStreak);
        
        var updatedExpert = await _context.Experts.FindAsync(expertId);
        Assert.NotNull(updatedExpert);
        Assert.Equal(ExpertTier.Bronze, updatedExpert.Tier);
    }

    [Fact]
    public async Task RecalculateStatisticsAsync_WithWonAndLostPositions_ShouldCalculateWinRate()
    {
        // Arrange
        var expertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expert = new ExpertEntity
        {
            Id = expertId,
            UserId = userId,
            DisplayName = "Test Expert",
            Status = ExpertStatus.Active,
            Tier = ExpertTier.Bronze
        };

        _context.Experts.Add(expert);
        await _context.SaveChangesAsync();

        var positions = new List<PositionEntity>
        {
            CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 2.0m, DateTime.UtcNow.AddDays(-10)),
            CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 1.5m, DateTime.UtcNow.AddDays(-9)),
            CreatePosition(userId, PositionStatus.Lost, PositionResult.Lost, 3.0m, DateTime.UtcNow.AddDays(-8)),
            CreatePosition(userId, PositionStatus.Lost, PositionResult.Lost, 2.5m, DateTime.UtcNow.AddDays(-7)),
            CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 1.8m, DateTime.UtcNow.AddDays(-6))
        };

        _expertRepositoryMock
            .Setup(r => r.GetByIdAsync(expertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expert);

        _positionRepositoryMock
            .Setup(r => r.GetByCreatorIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        // Act
        await _service.RecalculateStatisticsAsync(expertId);

        // Assert
        var statistics = await _context.ExpertStatistics
            .FirstOrDefaultAsync(s => s.ExpertId == expertId);

        Assert.NotNull(statistics);
        Assert.Equal(5, statistics.TotalPositions);
        Assert.Equal(3, statistics.WonPositions);
        Assert.Equal(2, statistics.LostPositions);
        Assert.Equal(0, statistics.VoidPositions);
        Assert.Equal(0, statistics.PendingPositions);
        // Win rate = (3 / (3 + 2)) * 100 = 60%
        Assert.Equal(60.00m, statistics.WinRate);
        // Average odds = (2.0 + 1.5 + 3.0 + 2.5 + 1.8) / 5 = 2.16
        Assert.Equal(2.16m, statistics.AverageOdds, 2);
    }

    [Fact]
    public async Task RecalculateStatisticsAsync_WithConsecutiveWins_ShouldCalculateStreak()
    {
        // Arrange
        var expertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expert = new ExpertEntity
        {
            Id = expertId,
            UserId = userId,
            DisplayName = "Test Expert",
            Status = ExpertStatus.Active,
            Tier = ExpertTier.Bronze
        };

        _context.Experts.Add(expert);
        await _context.SaveChangesAsync();

        var baseDate = DateTime.UtcNow.AddDays(-10);
        var positions = new List<PositionEntity>
        {
            CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 2.0m, baseDate),
            CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 1.5m, baseDate.AddDays(1)),
            CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 3.0m, baseDate.AddDays(2)),
            CreatePosition(userId, PositionStatus.Lost, PositionResult.Lost, 2.5m, baseDate.AddDays(3)),
            CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 1.8m, baseDate.AddDays(4))
        };

        _expertRepositoryMock
            .Setup(r => r.GetByIdAsync(expertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expert);

        _positionRepositoryMock
            .Setup(r => r.GetByCreatorIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        // Act
        await _service.RecalculateStatisticsAsync(expertId);

        // Assert
        var statistics = await _context.ExpertStatistics
            .FirstOrDefaultAsync(s => s.ExpertId == expertId);

        Assert.NotNull(statistics);
        // Current streak should be 1 (last position is a win)
        Assert.Equal(1, statistics.CurrentStreak);
        // Longest win streak should be 3 (first three positions)
        Assert.Equal(3, statistics.LongestWinStreak);
    }

    [Fact]
    public async Task RecalculateStatisticsAsync_WithTimeBasedPositions_ShouldCalculateTimeBasedWinRates()
    {
        // Arrange
        var expertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expert = new ExpertEntity
        {
            Id = expertId,
            UserId = userId,
            DisplayName = "Test Expert",
            Status = ExpertStatus.Active,
            Tier = ExpertTier.Bronze
        };

        _context.Experts.Add(expert);
        await _context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var positions = new List<PositionEntity>
        {
            // Last 7 days: 2 won, 1 lost = 66.67%
            CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 2.0m, now.AddDays(-5)),
            CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 1.5m, now.AddDays(-3)),
            CreatePosition(userId, PositionStatus.Lost, PositionResult.Lost, 3.0m, now.AddDays(-1)),
            // Last 30 days (but not 7): 1 won, 1 lost = 50%
            CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 2.5m, now.AddDays(-15)),
            CreatePosition(userId, PositionStatus.Lost, PositionResult.Lost, 1.8m, now.AddDays(-20)),
            // Last 90 days (but not 30): 1 won = 100%
            CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 2.2m, now.AddDays(-60))
        };

        _expertRepositoryMock
            .Setup(r => r.GetByIdAsync(expertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expert);

        _positionRepositoryMock
            .Setup(r => r.GetByCreatorIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        // Act
        await _service.RecalculateStatisticsAsync(expertId);

        // Assert
        var statistics = await _context.ExpertStatistics
            .FirstOrDefaultAsync(s => s.ExpertId == expertId);

        Assert.NotNull(statistics);
        // Last 7 days: 2 won, 1 lost = 66.67%
        Assert.Equal(66.67m, statistics.Last7DaysWinRate, 2);
        // Last 30 days: 3 won, 2 lost = 60%
        Assert.Equal(60.00m, statistics.Last30DaysWinRate, 2);
        // Last 90 days: 4 won, 2 lost = 66.67%
        Assert.Equal(66.67m, statistics.Last90DaysWinRate, 2);
    }

    [Fact]
    public async Task RecalculateStatisticsAsync_WithVoidPositions_ShouldExcludeFromWinRate()
    {
        // Arrange
        var expertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expert = new ExpertEntity
        {
            Id = expertId,
            UserId = userId,
            DisplayName = "Test Expert",
            Status = ExpertStatus.Active,
            Tier = ExpertTier.Bronze
        };

        _context.Experts.Add(expert);
        await _context.SaveChangesAsync();

        var positions = new List<PositionEntity>
        {
            CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 2.0m, DateTime.UtcNow.AddDays(-10)),
            CreatePosition(userId, PositionStatus.Lost, PositionResult.Lost, 1.5m, DateTime.UtcNow.AddDays(-9)),
            CreatePosition(userId, PositionStatus.Void, PositionResult.Void, 3.0m, DateTime.UtcNow.AddDays(-8)),
            CreatePosition(userId, PositionStatus.Void, PositionResult.Void, 2.5m, DateTime.UtcNow.AddDays(-7))
        };

        _expertRepositoryMock
            .Setup(r => r.GetByIdAsync(expertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expert);

        _positionRepositoryMock
            .Setup(r => r.GetByCreatorIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        // Act
        await _service.RecalculateStatisticsAsync(expertId);

        // Assert
        var statistics = await _context.ExpertStatistics
            .FirstOrDefaultAsync(s => s.ExpertId == expertId);

        Assert.NotNull(statistics);
        Assert.Equal(4, statistics.TotalPositions);
        Assert.Equal(1, statistics.WonPositions);
        Assert.Equal(1, statistics.LostPositions);
        Assert.Equal(2, statistics.VoidPositions);
        // Win rate = (1 / (1 + 1)) * 100 = 50% (void positions excluded)
        Assert.Equal(50.00m, statistics.WinRate);
    }

    [Fact]
    public async Task RecalculateStatisticsAsync_WithPendingPositions_ShouldCountButExcludeFromWinRate()
    {
        // Arrange
        var expertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expert = new ExpertEntity
        {
            Id = expertId,
            UserId = userId,
            DisplayName = "Test Expert",
            Status = ExpertStatus.Active,
            Tier = ExpertTier.Bronze
        };

        _context.Experts.Add(expert);
        await _context.SaveChangesAsync();

        var positions = new List<PositionEntity>
        {
            CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 2.0m, DateTime.UtcNow.AddDays(-10)),
            CreatePosition(userId, PositionStatus.Lost, PositionResult.Lost, 1.5m, DateTime.UtcNow.AddDays(-9)),
            CreatePosition(userId, PositionStatus.Pending, null, 3.0m, DateTime.UtcNow.AddDays(-1)),
            CreatePosition(userId, PositionStatus.Pending, null, 2.5m, DateTime.UtcNow)
        };

        _expertRepositoryMock
            .Setup(r => r.GetByIdAsync(expertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expert);

        _positionRepositoryMock
            .Setup(r => r.GetByCreatorIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        // Act
        await _service.RecalculateStatisticsAsync(expertId);

        // Assert
        var statistics = await _context.ExpertStatistics
            .FirstOrDefaultAsync(s => s.ExpertId == expertId);

        Assert.NotNull(statistics);
        Assert.Equal(4, statistics.TotalPositions);
        Assert.Equal(1, statistics.WonPositions);
        Assert.Equal(1, statistics.LostPositions);
        Assert.Equal(2, statistics.PendingPositions);
        // Win rate = (1 / (1 + 1)) * 100 = 50% (pending positions excluded)
        Assert.Equal(50.00m, statistics.WinRate);
    }

    [Theory]
    [InlineData(45.0, 25, ExpertTier.Bronze)] // Less than 50%, but has 25 positions
    [InlineData(55.0, 25, ExpertTier.Silver)] // 50-60%, has 25 positions
    [InlineData(65.0, 25, ExpertTier.Gold)] // 60-70%, has 25 positions
    [InlineData(75.0, 25, ExpertTier.Platinum)] // 70-80%, has 25 positions
    [InlineData(85.0, 25, ExpertTier.Diamond)] // 80%+, has 25 positions
    [InlineData(90.0, 15, ExpertTier.Bronze)] // 80%+ but less than 20 positions
    [InlineData(0.0, 10, ExpertTier.Bronze)] // 0% win rate
    public async Task RecalculateStatisticsAsync_ShouldDetermineCorrectTier(
        decimal winRate90Days, int totalPositions, ExpertTier expectedTier)
    {
        // Arrange
        var expertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expert = new ExpertEntity
        {
            Id = expertId,
            UserId = userId,
            DisplayName = "Test Expert",
            Status = ExpertStatus.Active,
            Tier = ExpertTier.Bronze
        };

        _context.Experts.Add(expert);
        await _context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var positions = new List<PositionEntity>();

        // Create positions to achieve the desired win rate in last 90 days
        var wonCount = (int)Math.Round(totalPositions * winRate90Days / 100);
        var lostCount = totalPositions - wonCount;

        for (int i = 0; i < wonCount; i++)
        {
            positions.Add(CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 2.0m, now.AddDays(-45)));
        }

        for (int i = 0; i < lostCount; i++)
        {
            positions.Add(CreatePosition(userId, PositionStatus.Lost, PositionResult.Lost, 2.0m, now.AddDays(-45)));
        }

        _expertRepositoryMock
            .Setup(r => r.GetByIdAsync(expertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expert);

        _positionRepositoryMock
            .Setup(r => r.GetByCreatorIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        // Act
        await _service.RecalculateStatisticsAsync(expertId);

        // Assert
        var updatedExpert = await _context.Experts.FindAsync(expertId);
        Assert.NotNull(updatedExpert);
        Assert.Equal(expectedTier, updatedExpert.Tier);
    }

    [Fact]
    public async Task RecalculateStatisticsAsync_WithExistingStatistics_ShouldUpdateNotCreate()
    {
        // Arrange
        var expertId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existingStatistics = new ExpertStatistics
        {
            ExpertId = expertId,
            TotalPositions = 10,
            WonPositions = 5,
            WinRate = 50.00m
        };

        var expert = new ExpertEntity
        {
            Id = expertId,
            UserId = userId,
            DisplayName = "Test Expert",
            Status = ExpertStatus.Active,
            Tier = ExpertTier.Bronze,
            Statistics = existingStatistics
        };

        _context.Experts.Add(expert);
        _context.ExpertStatistics.Add(existingStatistics);
        await _context.SaveChangesAsync();

        var positions = new List<PositionEntity>
        {
            CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 2.0m, DateTime.UtcNow.AddDays(-10)),
            CreatePosition(userId, PositionStatus.Won, PositionResult.Won, 1.5m, DateTime.UtcNow.AddDays(-9))
        };

        _expertRepositoryMock
            .Setup(r => r.GetByIdAsync(expertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expert);

        _positionRepositoryMock
            .Setup(r => r.GetByCreatorIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        // Act
        await _service.RecalculateStatisticsAsync(expertId);

        // Assert
        var statisticsCount = await _context.ExpertStatistics
            .CountAsync(s => s.ExpertId == expertId);
        
        Assert.Equal(1, statisticsCount); // Should still be only one record

        var statistics = await _context.ExpertStatistics
            .FirstOrDefaultAsync(s => s.ExpertId == expertId);

        Assert.NotNull(statistics);
        Assert.Equal(existingStatistics.ExpertId, statistics.ExpertId);
        Assert.Equal(2, statistics.TotalPositions); // Updated, not added
        Assert.Equal(2, statistics.WonPositions);
        Assert.Equal(100.00m, statistics.WinRate);
    }

    [Fact]
    public async Task RecalculateStatisticsAsync_ExpertNotFound_ShouldThrowException()
    {
        // Arrange
        var expertId = Guid.NewGuid();

        _expertRepositoryMock
            .Setup(r => r.GetByIdAsync(expertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpertEntity?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RecalculateStatisticsAsync(expertId));
    }

    private Domain.Entities.Position CreatePosition(
        Guid creatorId,
        PositionStatus status,
        PositionResult? result,
        decimal odds,
        DateTime createdAt)
    {
        var sportEvent = new SportEvent
        {
            Id = Guid.NewGuid(),
            ExternalEventId = Guid.NewGuid().ToString(),
            Sport = "Football",
            League = "Premier League",
            HomeTeam = "Team A",
            AwayTeam = "Team B",
            StartTimeUtc = DateTime.UtcNow.AddDays(1),
            StartTimeEpoch = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
            Status = EventStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };

        return new Domain.Entities.Position
        {
            Id = Guid.NewGuid(),
            CreatorId = creatorId,
            CreatorType = UserRole.Expert,
            SportEventId = sportEvent.Id,
            Market = "Match Result",
            Selection = "Home",
            Odds = odds,
            Status = status,
            Result = result,
            CreatedAt = createdAt,
            SportEvent = sportEvent
        };
    }
}


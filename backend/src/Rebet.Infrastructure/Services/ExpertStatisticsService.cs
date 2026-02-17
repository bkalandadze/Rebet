using Rebet.Application.Events;
using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Rebet.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Rebet.Infrastructure.Services;

public class ExpertStatisticsService : IExpertStatisticsService
{
    private readonly ApplicationDbContext _context;
    private readonly IPositionRepository _positionRepository;
    private readonly IExpertRepository _expertRepository;
    private readonly IMediator _mediator;

    public ExpertStatisticsService(
        ApplicationDbContext context,
        IPositionRepository positionRepository,
        IExpertRepository expertRepository,
        IMediator mediator)
    {
        _context = context;
        _positionRepository = positionRepository;
        _expertRepository = expertRepository;
        _mediator = mediator;
    }

    public async Task RecalculateStatisticsAsync(Guid expertId, CancellationToken cancellationToken = default)
    {
        // Get expert with statistics
        var expert = await _expertRepository.GetByIdAsync(expertId, cancellationToken);
        if (expert == null)
        {
            throw new InvalidOperationException($"Expert with ID {expertId} not found");
        }

        // Store previous values for event
        var previousStreak = expert.Statistics?.CurrentStreak;
        int? previousRank = null;

        // Check if expert was in top 10 before recalculation
        var leaderboardBefore = await _expertRepository.GetLeaderboardAsync(
            "winrate", null, null, null, 1, 10, null, cancellationToken);
        if (leaderboardBefore.Data.Any(e => e.Id == expertId))
        {
            previousRank = leaderboardBefore.Data
                .Select((e, index) => new { Expert = e, Rank = index + 1 })
                .FirstOrDefault(x => x.Expert.Id == expertId)?.Rank;
        }

        // Get all positions for this expert
        var positions = await _positionRepository.GetByCreatorIdAsync(expert.UserId, cancellationToken);

        // Calculate statistics
        var stats = CalculateStatistics(positions);

        // Get or create ExpertStatistics entity
        var expertStatistics = expert.Statistics;
        if (expertStatistics == null)
        {
            expertStatistics = new ExpertStatistics
            {
                ExpertId = expertId
            };
            _context.ExpertStatistics.Add(expertStatistics);
        }

        // Update statistics
        expertStatistics.TotalPositions = stats.TotalPositions;
        expertStatistics.WonPositions = stats.WonPositions;
        expertStatistics.LostPositions = stats.LostPositions;
        expertStatistics.VoidPositions = stats.VoidPositions;
        expertStatistics.PendingPositions = stats.PendingPositions;
        expertStatistics.WinRate = stats.WinRate;
        expertStatistics.AverageOdds = stats.AverageOdds;
        expertStatistics.CurrentStreak = stats.CurrentStreak;
        expertStatistics.LongestWinStreak = stats.LongestWinStreak;
        expertStatistics.Last7DaysWinRate = stats.Last7DaysWinRate;
        expertStatistics.Last30DaysWinRate = stats.Last30DaysWinRate;
        expertStatistics.Last90DaysWinRate = stats.Last90DaysWinRate;
        expertStatistics.LastCalculatedAt = DateTime.UtcNow;
        expertStatistics.UpdatedAt = DateTime.UtcNow;

        // Determine tier based on win rate (last 90 days)
        expert.Tier = DetermineTier(stats.Last90DaysWinRate, stats.TotalPositions);

        // Save changes
        await _context.SaveChangesAsync(cancellationToken);

        // Check current rank after recalculation
        int? currentRank = null;
        var leaderboardAfter = await _expertRepository.GetLeaderboardAsync(
            "winrate", null, null, null, 1, 10, null, cancellationToken);
        if (leaderboardAfter.Data.Any(e => e.Id == expertId))
        {
            currentRank = leaderboardAfter.Data
                .Select((e, index) => new { Expert = e, Rank = index + 1 })
                .FirstOrDefault(x => x.Expert.Id == expertId)?.Rank;
        }

        // Publish ExpertStatisticsRecalculatedEvent
        var statisticsRecalculatedEvent = new ExpertStatisticsRecalculatedEvent
        {
            ExpertId = expertId,
            PreviousStreak = previousStreak,
            CurrentStreak = stats.CurrentStreak,
            PreviousRank = previousRank,
            CurrentRank = currentRank,
            RecalculatedAt = DateTime.UtcNow
        };

        await _mediator.Publish(statisticsRecalculatedEvent, cancellationToken);
    }

    private StatisticsCalculationResult CalculateStatistics(IEnumerable<Position> positions)
    {
        var positionsList = positions.ToList();
        var now = DateTime.UtcNow;

        // Overall counts
        var totalPositions = positionsList.Count;
        var wonPositions = positionsList.Count(p => p.Status == PositionStatus.Won || p.Result == PositionResult.Won);
        var lostPositions = positionsList.Count(p => p.Status == PositionStatus.Lost || p.Result == PositionResult.Lost);
        var voidPositions = positionsList.Count(p => p.Status == PositionStatus.Void || p.Result == PositionResult.Void);
        var pendingPositions = positionsList.Count(p => p.Status == PositionStatus.Pending);

        // Win rate calculation: (won / (won + lost)) × 100
        var totalSettled = wonPositions + lostPositions;
        var winRate = totalSettled > 0 
            ? (decimal)wonPositions / totalSettled * 100 
            : 0.00m;

        // Average odds (only for settled positions)
        var settledPositions = positionsList
            .Where(p => p.Status != PositionStatus.Pending)
            .ToList();
        var averageOdds = settledPositions.Any()
            ? settledPositions.Average(p => p.Odds)
            : 0.00m;

        // Calculate streak (consecutive wins/losses)
        var streakResult = CalculateStreak(positionsList);

        // Time-based win rates
        var last7DaysWinRate = CalculateTimeBasedWinRate(positionsList, now.AddDays(-7));
        var last30DaysWinRate = CalculateTimeBasedWinRate(positionsList, now.AddDays(-30));
        var last90DaysWinRate = CalculateTimeBasedWinRate(positionsList, now.AddDays(-90));

        return new StatisticsCalculationResult
        {
            TotalPositions = totalPositions,
            WonPositions = wonPositions,
            LostPositions = lostPositions,
            VoidPositions = voidPositions,
            PendingPositions = pendingPositions,
            WinRate = winRate,
            AverageOdds = averageOdds,
            CurrentStreak = streakResult.CurrentStreak,
            LongestWinStreak = streakResult.LongestWinStreak,
            Last7DaysWinRate = last7DaysWinRate,
            Last30DaysWinRate = last30DaysWinRate,
            Last90DaysWinRate = last90DaysWinRate
        };
    }

    private StreakResult CalculateStreak(List<Position> positions)
    {
        if (!positions.Any())
        {
            return new StreakResult { CurrentStreak = 0, LongestWinStreak = 0 };
        }

        // Order by creation date (oldest first) to calculate streak chronologically
        var orderedPositions = positions
            .Where(p => p.Status != PositionStatus.Pending)
            .OrderBy(p => p.CreatedAt)
            .ToList();

        if (!orderedPositions.Any())
        {
            return new StreakResult { CurrentStreak = 0, LongestWinStreak = 0 };
        }

        int currentStreak = 0;
        int longestWinStreak = 0;
        int currentWinStreak = 0;

        foreach (var position in orderedPositions)
        {
            var isWin = position.Status == PositionStatus.Won || position.Result == PositionResult.Won;
            var isLoss = position.Status == PositionStatus.Lost || position.Result == PositionResult.Lost;
            var isVoid = position.Status == PositionStatus.Void || position.Result == PositionResult.Void;

            // Skip void positions for streak calculation
            if (isVoid)
            {
                continue;
            }

            if (isWin)
            {
                if (currentStreak >= 0)
                {
                    currentStreak++;
                    currentWinStreak++;
                }
                else
                {
                    currentStreak = 1;
                    currentWinStreak = 1;
                }
                longestWinStreak = Math.Max(longestWinStreak, currentWinStreak);
            }
            else if (isLoss)
            {
                if (currentStreak <= 0)
                {
                    currentStreak--;
                }
                else
                {
                    currentStreak = -1;
                }
                currentWinStreak = 0;
            }
        }

        return new StreakResult
        {
            CurrentStreak = currentStreak,
            LongestWinStreak = longestWinStreak
        };
    }

    private decimal CalculateTimeBasedWinRate(List<Position> positions, DateTime cutoffDate)
    {
        var positionsInPeriod = positions
            .Where(p => p.CreatedAt >= cutoffDate)
            .Where(p => p.Status != PositionStatus.Pending)
            .ToList();

        var wonInPeriod = positionsInPeriod.Count(p => 
            p.Status == PositionStatus.Won || p.Result == PositionResult.Won);
        var lostInPeriod = positionsInPeriod.Count(p => 
            p.Status == PositionStatus.Lost || p.Result == PositionResult.Lost);

        var totalSettled = wonInPeriod + lostInPeriod;
        return totalSettled > 0 
            ? (decimal)wonInPeriod / totalSettled * 100 
            : 0.00m;
    }

    private ExpertTier DetermineTier(decimal winRate, int totalPositions)
    {
        // Minimum 20 positions to qualify for higher tiers (per business rules)
        if (totalPositions < 20)
        {
            return ExpertTier.Bronze;
        }

        // Tier determination based on last 90 days win rate
        return winRate switch
        {
            >= 80.0m => ExpertTier.Diamond,
            >= 70.0m => ExpertTier.Platinum,
            >= 60.0m => ExpertTier.Gold,
            >= 50.0m => ExpertTier.Silver,
            _ => ExpertTier.Bronze
        };
    }

    private class StatisticsCalculationResult
    {
        public int TotalPositions { get; set; }
        public int WonPositions { get; set; }
        public int LostPositions { get; set; }
        public int VoidPositions { get; set; }
        public int PendingPositions { get; set; }
        public decimal WinRate { get; set; }
        public decimal AverageOdds { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestWinStreak { get; set; }
        public decimal Last7DaysWinRate { get; set; }
        public decimal Last30DaysWinRate { get; set; }
        public decimal Last90DaysWinRate { get; set; }
    }

    private class StreakResult
    {
        public int CurrentStreak { get; set; }
        public int LongestWinStreak { get; set; }
    }
}


using Rebet.Application.Events;
using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Rebet.Infrastructure.BackgroundJobs.SettlementStrategies;
using Rebet.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Rebet.Infrastructure.BackgroundJobs;

public class SettlePositionsJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IExpertStatisticsService _expertStatisticsService;
    private readonly IMediator _mediator;
    private readonly ILogger<SettlePositionsJob> _logger;
    private readonly SettlementStrategyFactory _strategyFactory;

    public SettlePositionsJob(
        ApplicationDbContext dbContext,
        IExpertStatisticsService expertStatisticsService,
        IMediator mediator,
        ILogger<SettlePositionsJob> logger)
    {
        _dbContext = dbContext;
        _expertStatisticsService = expertStatisticsService;
        _mediator = mediator;
        _logger = logger;
        _strategyFactory = new SettlementStrategyFactory(logger);
    }

    public async Task SettlePositions()
    {
        try
        {
            _logger.LogInformation("Starting settle positions job at {Time}", DateTime.UtcNow);

            var now = DateTime.UtcNow;
            var tenMinutesAgo = now.AddMinutes(-10);

            // 1. Query EventResults settled in last 10 minutes
            var recentEventResults = await _dbContext.EventResults
                .Where(er => er.SettledAt >= tenMinutesAgo && er.SettledAt <= now)
                .Include(er => er.SportEvent)
                .ToListAsync();

            if (!recentEventResults.Any())
            {
                _logger.LogInformation("No event results found settled in the last 10 minutes");
                return;
            }

            _logger.LogInformation("Found {Count} event results settled in last 10 minutes", recentEventResults.Count);

            var eventIds = recentEventResults.Select(er => er.SportEventId).ToList();

            // 2. Find all Positions with status = Pending for those events
            var pendingPositions = await _dbContext.Positions
                .Where(p => eventIds.Contains(p.SportEventId) && 
                           p.Status == PositionStatus.Pending && 
                           !p.IsDeleted)
                .Include(p => p.SportEvent)
                    .ThenInclude(se => se.Result)
                .Include(p => p.Creator)
                    .ThenInclude(c => c.Expert)
                .ToListAsync();

            if (!pendingPositions.Any())
            {
                _logger.LogInformation("No pending positions found for settled events");
                return;
            }

            _logger.LogInformation("Found {Count} pending positions to settle", pendingPositions.Count);

            var positionsToUpdate = new List<Position>();
            var expertIdsToRecalculate = new HashSet<Guid>();
            var positionSettledEvents = new List<PositionSettledEvent>();

            // 3. For each position: determine win/loss and update
            foreach (var position in pendingPositions)
            {
                var eventResult = recentEventResults.FirstOrDefault(er => er.SportEventId == position.SportEventId);
                if (eventResult == null)
                {
                    _logger.LogWarning("Event result not found for position {PositionId}, event {EventId}", 
                        position.Id, position.SportEventId);
                    continue;
                }

                var settlementResult = DeterminePositionResult(position, eventResult);

                // Update position status and result
                position.Result = settlementResult.Result;
                position.Status = settlementResult.Status;
                position.SettledAt = now;

                positionsToUpdate.Add(position);

                // Track expert for statistics recalculation
                if (position.CreatorType == UserRole.Expert && position.Creator.Expert != null)
                {
                    expertIdsToRecalculate.Add(position.Creator.Expert.Id);
                }

                // Create PositionSettledEvent for event handler to process
                var positionSettledEvent = new PositionSettledEvent
                {
                    PositionId = position.Id,
                    CreatorId = position.CreatorId,
                    CreatorType = position.CreatorType,
                    ExpertId = position.Creator.Expert?.Id,
                    Result = settlementResult.Result,
                    Odds = position.Odds,
                    Market = position.Market,
                    Selection = position.Selection,
                    SettledAt = now
                };
                positionSettledEvents.Add(positionSettledEvent);

                _logger.LogInformation(
                    "Settled position {PositionId}: {Result} (Market: {Market}, Selection: {Selection})",
                    position.Id, settlementResult.Result, position.Market, position.Selection);
            }

            // 4. Batch update positions
            if (positionsToUpdate.Any())
            {
                _dbContext.Positions.UpdateRange(positionsToUpdate);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Updated {Count} positions", positionsToUpdate.Count);
            }

            // 5. Publish PositionSettledEvent for each settled position
            foreach (var positionSettledEvent in positionSettledEvents)
            {
                try
                {
                    await _mediator.Publish(positionSettledEvent, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing PositionSettledEvent for position {PositionId}: {Error}",
                        positionSettledEvent.PositionId, ex.Message);
                }
            }

            // 6. For each affected expert: Call ExpertStatisticsService.RecalculateStatistics
            foreach (var expertId in expertIdsToRecalculate)
            {
                try
                {
                    await _expertStatisticsService.RecalculateStatisticsAsync(expertId);
                    _logger.LogInformation("Recalculated statistics for expert {ExpertId}", expertId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error recalculating statistics for expert {ExpertId}: {Error}", 
                        expertId, ex.Message);
                }
            }

            // 7. Send notifications to position followers
            // Note: Notification service implementation is pending
            // When implemented, send notifications to:
            // - Position creator (if expert, notify subscribers)
            // - Ticket followers (if position is part of a ticket)
            // For now, we'll log that notifications should be sent
            var positionsWithFollowers = positionsToUpdate
                .Where(p => p.CreatorType == UserRole.Expert)
                .ToList();
            
            if (positionsWithFollowers.Any())
            {
                _logger.LogInformation(
                    "Should send notifications for {Count} positions with followers", 
                    positionsWithFollowers.Count);
            }

            _logger.LogInformation(
                "Settle positions job completed. Settled: {Settled}, Experts updated: {Experts}",
                positionsToUpdate.Count, expertIdsToRecalculate.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SettlePositions job: {Error}", ex.Message);
            throw; // Re-throw to let Hangfire handle retry
        }
    }

    private SettlementResult DeterminePositionResult(Position position, EventResult eventResult)
    {
        // Check for void conditions first
        if (eventResult.SportEvent.Status == EventStatus.Cancelled)
        {
            return new SettlementResult
            {
                Result = PositionResult.Void,
                Status = PositionStatus.Void
            };
        }

        // Parse market results JSON if available
        MarketResults? marketResults = ParseMarketResults(eventResult);

        // Get appropriate strategy for market type
        var strategy = _strategyFactory.GetStrategy(position.Market);
        return strategy.DetermineResult(position, eventResult, marketResults);
    }

    private MarketResults? ParseMarketResults(EventResult eventResult)
    {
        if (string.IsNullOrEmpty(eventResult.MarketResultsJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<MarketResults>(eventResult.MarketResultsJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse market results JSON for event {EventId}",
                eventResult.SportEventId);
            return null;
        }
    }

}


using Rebet.Application.Events;
using Rebet.Application.Interfaces;
using Rebet.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Rebet.Application.EventHandlers;

/// <summary>
/// Handles cache invalidation for domain events.
/// Invalidates cache entries based on the type of event that occurred.
/// </summary>
public class CacheInvalidationEventHandler :
    INotificationHandler<PositionCreatedEvent>,
    INotificationHandler<PositionSettledEvent>,
    INotificationHandler<VoteCastEvent>,
    INotificationHandler<ExpertStatisticsRecalculatedEvent>
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheInvalidationEventHandler> _logger;

    public CacheInvalidationEventHandler(
        ICacheService cacheService,
        ILogger<CacheInvalidationEventHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Handle(PositionCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Invalidating cache for PositionCreatedEvent: PositionId={PositionId}",
                notification.PositionId);

            // Invalidate top positions cache (pattern-based)
            await _cacheService.InvalidateByPatternAsync("positions:top:*", cancellationToken);
            
            _logger.LogInformation(
                "Cache invalidated for PositionCreatedEvent: PositionId={PositionId}",
                notification.PositionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error invalidating cache for PositionCreatedEvent: PositionId={PositionId}, Error={Error}",
                notification.PositionId, ex.Message);
            // Don't throw - cache invalidation failures shouldn't break the main flow
        }
    }

    public async Task Handle(PositionSettledEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Invalidating cache for PositionSettledEvent: PositionId={PositionId}, ExpertId={ExpertId}",
                notification.PositionId, notification.ExpertId);

            // Invalidate specific position detail
            await _cacheService.InvalidateAsync($"position:detail:{notification.PositionId}", cancellationToken);

            // Invalidate top positions cache (pattern-based)
            await _cacheService.InvalidateByPatternAsync("positions:top:*", cancellationToken);

            // Invalidate expert statistics if expert position
            if (notification.ExpertId.HasValue)
            {
                await _cacheService.InvalidateAsync($"expert:stats:{notification.ExpertId.Value}", cancellationToken);
                await _cacheService.InvalidateAsync($"expert:profile:{notification.ExpertId.Value}", cancellationToken);
            }

            // Invalidate expert leaderboard (pattern-based)
            await _cacheService.InvalidateByPatternAsync("experts:leaderboard:*", cancellationToken);

            _logger.LogInformation(
                "Cache invalidated for PositionSettledEvent: PositionId={PositionId}, ExpertId={ExpertId}",
                notification.PositionId, notification.ExpertId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error invalidating cache for PositionSettledEvent: PositionId={PositionId}, Error={Error}",
                notification.PositionId, ex.Message);
            // Don't throw - cache invalidation failures shouldn't break the main flow
        }
    }

    public async Task Handle(VoteCastEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Invalidating cache for VoteCastEvent: VoteableType={VoteableType}, VoteableId={VoteableId}",
                notification.VoteableType, notification.VoteableId);

            // Invalidate specific item cache based on voteable type
            switch (notification.VoteableType)
            {
                case VoteableType.Position:
                    await _cacheService.InvalidateAsync($"position:detail:{notification.VoteableId}", cancellationToken);
                    // Also invalidate top positions as vote counts affect sorting
                    await _cacheService.InvalidateByPatternAsync("positions:top:*", cancellationToken);
                    break;

                case VoteableType.Ticket:
                    await _cacheService.InvalidateAsync($"ticket:detail:{notification.VoteableId}", cancellationToken);
                    // Also invalidate top tickets as vote counts affect sorting
                    await _cacheService.InvalidateByPatternAsync("tickets:top:*", cancellationToken);
                    break;

                case VoteableType.Expert:
                    await _cacheService.InvalidateAsync($"expert:profile:{notification.VoteableId}", cancellationToken);
                    // Also invalidate leaderboard as vote counts affect sorting
                    await _cacheService.InvalidateByPatternAsync("experts:leaderboard:*", cancellationToken);
                    break;
            }

            _logger.LogInformation(
                "Cache invalidated for VoteCastEvent: VoteableType={VoteableType}, VoteableId={VoteableId}",
                notification.VoteableType, notification.VoteableId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error invalidating cache for VoteCastEvent: VoteableType={VoteableType}, VoteableId={VoteableId}, Error={Error}",
                notification.VoteableType, notification.VoteableId, ex.Message);
            // Don't throw - cache invalidation failures shouldn't break the main flow
        }
    }

    public async Task Handle(ExpertStatisticsRecalculatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Invalidating cache for ExpertStatisticsRecalculatedEvent: ExpertId={ExpertId}",
                notification.ExpertId);

            // Invalidate expert profile
            await _cacheService.InvalidateAsync($"expert:profile:{notification.ExpertId}", cancellationToken);

            // Invalidate expert statistics
            await _cacheService.InvalidateAsync($"expert:stats:{notification.ExpertId}", cancellationToken);

            // Invalidate expert leaderboard (pattern-based)
            await _cacheService.InvalidateByPatternAsync("experts:leaderboard:*", cancellationToken);

            _logger.LogInformation(
                "Cache invalidated for ExpertStatisticsRecalculatedEvent: ExpertId={ExpertId}",
                notification.ExpertId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error invalidating cache for ExpertStatisticsRecalculatedEvent: ExpertId={ExpertId}, Error={Error}",
                notification.ExpertId, ex.Message);
            // Don't throw - cache invalidation failures shouldn't break the main flow
        }
    }
}


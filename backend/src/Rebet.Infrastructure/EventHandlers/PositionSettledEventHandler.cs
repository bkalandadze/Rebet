using Rebet.Application.Events;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Rebet.Infrastructure.Hubs;
using Rebet.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rebet.Infrastructure.EventHandlers;

public class PositionSettledEventHandler : INotificationHandler<PositionSettledEvent>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHubContext<NewsfeedHub> _hubContext;
    private readonly ILogger<PositionSettledEventHandler> _logger;

    public PositionSettledEventHandler(
        ApplicationDbContext dbContext,
        IHubContext<NewsfeedHub> hubContext,
        ILogger<PositionSettledEventHandler> logger)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(PositionSettledEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Only create newsfeed item if position won and odds >= 2.0
            if (notification.Result != PositionResult.Won || notification.Odds < 2.0m)
            {
                _logger.LogDebug(
                    "Position {PositionId} does not meet criteria for newsfeed (result: {Result}, odds: {Odds})",
                    notification.PositionId, notification.Result, notification.Odds);
                return;
            }

            // Only create newsfeed item for expert positions
            if (notification.CreatorType != UserRole.Expert || !notification.ExpertId.HasValue)
            {
                _logger.LogDebug(
                    "Position {PositionId} is not from an expert, skipping newsfeed item",
                    notification.PositionId);
                return;
            }

            // Get the expert
            var expert = await _dbContext.Experts
                .FirstOrDefaultAsync(e => e.Id == notification.ExpertId.Value && !e.IsDeleted, cancellationToken);

            if (expert == null)
            {
                _logger.LogWarning(
                    "Expert {ExpertId} not found when creating newsfeed item for settled position {PositionId}",
                    notification.ExpertId, notification.PositionId);
                return;
            }

            // Get position for additional context
            var position = await _dbContext.Positions
                .Include(p => p.SportEvent)
                .FirstOrDefaultAsync(p => p.Id == notification.PositionId && !p.IsDeleted, cancellationToken);

            if (position == null)
            {
                _logger.LogWarning(
                    "Position {PositionId} not found when creating newsfeed item",
                    notification.PositionId);
                return;
            }

            // Create newsfeed item
            var newsfeedItem = new NewsfeedItem
            {
                Id = Guid.NewGuid(),
                Type = NewsfeedType.SuccessfulPrediction,
                Title = $"{expert.DisplayName}'s prediction WON!",
                Description = $"{notification.Selection} @ {notification.Odds:F2}",
                ExpertId = expert.Id,
                PositionId = notification.PositionId,
                ActionUrl = $"/positions/{notification.PositionId}",
                CreatedAt = notification.SettledAt
            };

            // Add metadata JSON
            var metadata = new
            {
                sport = position.SportEvent?.Sport,
                league = position.SportEvent?.League,
                homeTeam = position.SportEvent?.HomeTeam,
                awayTeam = position.SportEvent?.AwayTeam,
                market = notification.Market,
                selection = notification.Selection,
                odds = notification.Odds,
                result = "Won"
            };
            newsfeedItem.MetadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);

            await _dbContext.NewsfeedItems.AddAsync(newsfeedItem, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created newsfeed item {NewsfeedItemId} for successful prediction {PositionId} by expert {ExpertId}",
                newsfeedItem.Id, notification.PositionId, expert.Id);

            // Broadcast to SignalR groups
            await _hubContext.Clients.Group("newsfeed").SendAsync("NewsfeedItemCreated", new
            {
                id = newsfeedItem.Id,
                type = newsfeedItem.Type,
                title = newsfeedItem.Title,
                description = newsfeedItem.Description,
                expertId = newsfeedItem.ExpertId,
                positionId = newsfeedItem.PositionId,
                actionUrl = newsfeedItem.ActionUrl,
                createdAt = newsfeedItem.CreatedAt
            }, cancellationToken);

            await _hubContext.Clients.Group($"expert_{expert.Id}").SendAsync("NewsfeedItemCreated", new
            {
                id = newsfeedItem.Id,
                type = newsfeedItem.Type,
                title = newsfeedItem.Title,
                description = newsfeedItem.Description,
                expertId = newsfeedItem.ExpertId,
                positionId = newsfeedItem.PositionId,
                actionUrl = newsfeedItem.ActionUrl,
                createdAt = newsfeedItem.CreatedAt
            }, cancellationToken);

            // Also broadcast to sport-specific group if sport event exists
            if (position.SportEvent != null)
            {
                await _hubContext.Clients.Group($"sport_{position.SportEvent.Sport.ToLowerInvariant()}")
                    .SendAsync("NewsfeedItemCreated", new
                    {
                        id = newsfeedItem.Id,
                        type = newsfeedItem.Type,
                        title = newsfeedItem.Title,
                        description = newsfeedItem.Description,
                        expertId = newsfeedItem.ExpertId,
                        positionId = newsfeedItem.PositionId,
                        actionUrl = newsfeedItem.ActionUrl,
                        createdAt = newsfeedItem.CreatedAt
                    }, cancellationToken);
            }

            _logger.LogInformation(
                "Broadcasted newsfeed item {NewsfeedItemId} to SignalR groups",
                newsfeedItem.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling PositionSettledEvent for position {PositionId}: {Error}",
                notification.PositionId, ex.Message);
            // Don't throw - we don't want to break the settlement flow
        }
    }
}


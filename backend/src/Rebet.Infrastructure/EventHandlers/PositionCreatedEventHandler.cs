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

public class PositionCreatedEventHandler : INotificationHandler<PositionCreatedEvent>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHubContext<NewsfeedHub> _hubContext;
    private readonly ILogger<PositionCreatedEventHandler> _logger;

    public PositionCreatedEventHandler(
        ApplicationDbContext dbContext,
        IHubContext<NewsfeedHub> hubContext,
        ILogger<PositionCreatedEventHandler> logger)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(PositionCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Only create newsfeed item if creator is an Expert
            if (notification.CreatorType != "Expert")
            {
                _logger.LogDebug(
                    "Position {PositionId} created by non-expert user {CreatorId}, skipping newsfeed item",
                    notification.PositionId, notification.CreatorId);
                return;
            }

            // Get the expert to check subscriber count and verification status
            var expert = await _dbContext.Experts
                .Include(e => e.Statistics)
                .FirstOrDefaultAsync(e => e.UserId == notification.CreatorId && !e.IsDeleted, cancellationToken);

            if (expert == null)
            {
                _logger.LogWarning(
                    "Expert not found for user {CreatorId} when creating newsfeed item for position {PositionId}",
                    notification.CreatorId, notification.PositionId);
                return;
            }

            // Count active subscribers
            var subscriberCount = await _dbContext.Set<Subscription>()
                .CountAsync(s => s.ExpertId == expert.Id &&
                                s.Status == SubscriptionStatus.Active &&
                                !s.IsDeleted, cancellationToken);

            // Use TotalSubscribers from statistics if available, otherwise use count
            var totalSubscribers = expert.Statistics?.TotalSubscribers ?? subscriberCount;

            // Condition: Expert has 100+ subscribers OR is verified
            if (totalSubscribers < 100 && !expert.IsVerified)
            {
                _logger.LogDebug(
                    "Expert {ExpertId} does not meet criteria for newsfeed (subscribers: {Subscribers}, verified: {Verified})",
                    expert.Id, totalSubscribers, expert.IsVerified);
                return;
            }

            // Get sport event for additional context
            var sportEvent = await _dbContext.SportEvents
                .FirstOrDefaultAsync(se => se.Id == notification.SportEventId && !se.IsDeleted, cancellationToken);

            // Create newsfeed item
            var newsfeedItem = new NewsfeedItem
            {
                Id = Guid.NewGuid(),
                Type = NewsfeedType.NewPosition,
                Title = $"{expert.DisplayName} posted new position",
                Description = $"{notification.Selection} @ {notification.Odds:F2}",
                ExpertId = expert.Id,
                PositionId = notification.PositionId,
                ActionUrl = $"/positions/{notification.PositionId}",
                CreatedAt = notification.CreatedAt
            };

            // Add metadata JSON if sport event exists
            if (sportEvent != null)
            {
                var metadata = new
                {
                    sport = sportEvent.Sport,
                    league = sportEvent.League,
                    homeTeam = sportEvent.HomeTeam,
                    awayTeam = sportEvent.AwayTeam,
                    market = notification.Market,
                    selection = notification.Selection,
                    odds = notification.Odds
                };
                newsfeedItem.MetadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
            }

            await _dbContext.NewsfeedItems.AddAsync(newsfeedItem, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Created newsfeed item {NewsfeedItemId} for position {PositionId} by expert {ExpertId}",
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
            if (sportEvent != null)
            {
                await _hubContext.Clients.Group($"sport_{sportEvent.Sport.ToLowerInvariant()}")
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
                "Error handling PositionCreatedEvent for position {PositionId}: {Error}",
                notification.PositionId, ex.Message);
            // Don't throw - we don't want to break the position creation flow
        }
    }
}


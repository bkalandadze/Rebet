using Rebet.Application.Events;
using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Rebet.Infrastructure.Hubs;
using Rebet.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rebet.Infrastructure.EventHandlers;

public class ExpertAchievementEventHandler : INotificationHandler<ExpertStatisticsRecalculatedEvent>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHubContext<NewsfeedHub> _hubContext;
    private readonly IExpertRepository _expertRepository;
    private readonly ILogger<ExpertAchievementEventHandler> _logger;

    public ExpertAchievementEventHandler(
        ApplicationDbContext dbContext,
        IHubContext<NewsfeedHub> hubContext,
        IExpertRepository expertRepository,
        ILogger<ExpertAchievementEventHandler> logger)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _expertRepository = expertRepository;
        _logger = logger;
    }

    public async Task Handle(ExpertStatisticsRecalculatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Get expert with statistics
            var expert = await _dbContext.Experts
                .Include(e => e.Statistics)
                .FirstOrDefaultAsync(e => e.Id == notification.ExpertId && !e.IsDeleted, cancellationToken);

            if (expert == null || expert.Statistics == null)
            {
                _logger.LogWarning(
                    "Expert {ExpertId} or statistics not found when checking achievements",
                    notification.ExpertId);
                return;
            }

            var achievements = new List<(NewsfeedType Type, string Title, string Description)>();

            // Check for win streak achievements
            if (notification.PreviousStreak.HasValue)
            {
                // 5-win streak achievement
                if (notification.PreviousStreak.Value < 5 && notification.CurrentStreak >= 5)
                {
                    achievements.Add((
                        NewsfeedType.ExpertAchievement,
                        $"{expert.DisplayName} reached 5 wins in a row!",
                        "Impressive winning streak!"
                    ));
                }

                // 10-win streak achievement
                if (notification.PreviousStreak.Value < 10 && notification.CurrentStreak >= 10)
                {
                    achievements.Add((
                        NewsfeedType.ExpertAchievement,
                        $"{expert.DisplayName} reached 10 wins in a row!",
                        "Outstanding winning streak!"
                    ));
                }
            }

            // Check for top 10 entry
            if (notification.PreviousRank.HasValue && notification.CurrentRank.HasValue)
            {
                // Entered top 10 (wasn't in top 10 before, now is)
                if (notification.PreviousRank.Value > 10 && notification.CurrentRank.Value <= 10)
                {
                    achievements.Add((
                        NewsfeedType.TopListChange,
                        $"{expert.DisplayName} entered Top 10 experts!",
                        $"Ranked #{notification.CurrentRank.Value} on the leaderboard"
                    ));
                }
            }
            else if (!notification.PreviousRank.HasValue && notification.CurrentRank.HasValue)
            {
                // First time entering top 10
                if (notification.CurrentRank.Value <= 10)
                {
                    achievements.Add((
                        NewsfeedType.TopListChange,
                        $"{expert.DisplayName} entered Top 10 experts!",
                        $"Ranked #{notification.CurrentRank.Value} on the leaderboard"
                    ));
                }
            }

            // Create newsfeed items for each achievement
            foreach (var (type, title, description) in achievements)
            {
                var newsfeedItem = new NewsfeedItem
                {
                    Id = Guid.NewGuid(),
                    Type = type,
                    Title = title,
                    Description = description,
                    ExpertId = expert.Id,
                    ActionUrl = $"/experts/{expert.Id}",
                    CreatedAt = notification.RecalculatedAt
                };

                // Add metadata JSON
                var metadata = new
                {
                    achievementType = type == NewsfeedType.ExpertAchievement ? "streak" : "top10",
                    currentStreak = notification.CurrentStreak,
                    currentRank = notification.CurrentRank,
                    winRate = expert.Statistics.WinRate,
                    tier = expert.Tier.ToString()
                };
                newsfeedItem.MetadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);

                await _dbContext.NewsfeedItems.AddAsync(newsfeedItem, cancellationToken);

                _logger.LogInformation(
                    "Created newsfeed item {NewsfeedItemId} for achievement: {Title} (Expert: {ExpertId})",
                    newsfeedItem.Id, title, expert.Id);

                // Broadcast to SignalR groups
                await _hubContext.Clients.Group("newsfeed").SendAsync("NewsfeedItemCreated", new
                {
                    id = newsfeedItem.Id,
                    type = newsfeedItem.Type,
                    title = newsfeedItem.Title,
                    description = newsfeedItem.Description,
                    expertId = newsfeedItem.ExpertId,
                    positionId = (Guid?)null,
                    ticketId = (Guid?)null,
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
                    positionId = (Guid?)null,
                    ticketId = (Guid?)null,
                    actionUrl = newsfeedItem.ActionUrl,
                    createdAt = newsfeedItem.CreatedAt
                }, cancellationToken);

                _logger.LogInformation(
                    "Broadcasted newsfeed item {NewsfeedItemId} to SignalR groups",
                    newsfeedItem.Id);
            }

            // Save all newsfeed items at once
            if (achievements.Any())
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                    "Created {Count} achievement newsfeed items for expert {ExpertId}",
                    achievements.Count, expert.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling ExpertStatisticsRecalculatedEvent for expert {ExpertId}: {Error}",
                notification.ExpertId, ex.Message);
            // Don't throw - we don't want to break the statistics recalculation flow
        }
    }
}


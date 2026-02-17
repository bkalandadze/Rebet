using Microsoft.AspNetCore.SignalR;

namespace Rebet.Infrastructure.Hubs;

/// <summary>
/// SignalR Hub for real-time newsfeed updates
/// </summary>
public class NewsfeedHub : Hub
{
    /// <summary>
    /// Subscribe to general newsfeed updates
    /// </summary>
    public async Task SubscribeToNewsfeed()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "newsfeed");
    }

    /// <summary>
    /// Subscribe to updates for a specific expert
    /// </summary>
    /// <param name="expertId">The expert's unique identifier</param>
    public async Task SubscribeToExpert(Guid expertId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"expert_{expertId}");
    }

    /// <summary>
    /// Subscribe to updates for a specific sport
    /// </summary>
    /// <param name="sport">The sport name (e.g., "football", "basketball")</param>
    public async Task SubscribeToSport(string sport)
    {
        if (string.IsNullOrWhiteSpace(sport))
        {
            throw new ArgumentException("Sport name cannot be null or empty.", nameof(sport));
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"sport_{sport.ToLowerInvariant()}");
    }

    /// <summary>
    /// Unsubscribe from general newsfeed updates
    /// </summary>
    public async Task UnsubscribeFromNewsfeed()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "newsfeed");
    }

    /// <summary>
    /// Unsubscribe from updates for a specific expert
    /// </summary>
    /// <param name="expertId">The expert's unique identifier</param>
    public async Task UnsubscribeFromExpert(Guid expertId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"expert_{expertId}");
    }

    /// <summary>
    /// Unsubscribe from updates for a specific sport
    /// </summary>
    /// <param name="sport">The sport name (e.g., "football", "basketball")</param>
    public async Task UnsubscribeFromSport(string sport)
    {
        if (string.IsNullOrWhiteSpace(sport))
        {
            throw new ArgumentException("Sport name cannot be null or empty.", nameof(sport));
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sport_{sport.ToLowerInvariant()}");
    }

    /// <summary>
    /// Unsubscribe from all groups
    /// Note: SignalR doesn't provide a built-in way to get all groups for a connection.
    /// This method serves as a placeholder. In production, you might want to track
    /// subscriptions in Redis or database, or have clients call individual unsubscribe methods.
    /// </summary>
    public async Task UnsubscribeFromAll()
    {
        // Note: SignalR doesn't expose a way to enumerate all groups for a connection.
        // Clients should call individual unsubscribe methods, or you can track subscriptions
        // in Redis/database for a more robust implementation.
        // This method is provided for convenience but may not remove from all groups.
        await Task.CompletedTask;
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Clean up: remove from all groups on disconnect
        await UnsubscribeFromAll();
        await base.OnDisconnectedAsync(exception);
    }
}


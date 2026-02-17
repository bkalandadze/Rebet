namespace Rebet.Infrastructure.Services;

/// <summary>
/// Constants for cache keys used throughout the application.
/// </summary>
public static class CacheKeys
{
    private const string Prefix = "Rebet";

    // User cache keys
    public static string User(Guid userId) => $"{Prefix}:User:{userId}";
    public static string UserByEmail(string email) => $"{Prefix}:User:Email:{email}";
    public static string UserPattern => $"{Prefix}:User:*";

    // Expert cache keys
    public static string Expert(Guid expertId) => $"{Prefix}:Expert:{expertId}";
    public static string ExpertStatistics(Guid expertId) => $"{Prefix}:Expert:Statistics:{expertId}";
    public static string ExpertLeaderboard => $"{Prefix}:Expert:Leaderboard";
    public static string ExpertPattern => $"{Prefix}:Expert:*";

    // Position cache keys
    public static string Position(Guid positionId) => $"{Prefix}:Position:{positionId}";
    public static string TopPositions => $"{Prefix}:Position:Top";
    public static string PositionsByExpert(Guid expertId) => $"{Prefix}:Position:Expert:{expertId}";
    public static string PositionsByEvent(Guid eventId) => $"{Prefix}:Position:Event:{eventId}";
    public static string PositionPattern => $"{Prefix}:Position:*";

    // Ticket cache keys
    public static string Ticket(Guid ticketId) => $"{Prefix}:Ticket:{ticketId}";
    public static string TicketsByUser(Guid userId) => $"{Prefix}:Ticket:User:{userId}";
    public static string TicketPattern => $"{Prefix}:Ticket:*";

    // Sport Event cache keys
    public static string SportEvent(Guid eventId) => $"{Prefix}:SportEvent:{eventId}";
    public static string HotEvents => $"{Prefix}:SportEvent:Hot";
    public static string SportEventPattern => $"{Prefix}:SportEvent:*";

    // Newsfeed cache keys
    public static string Newsfeed => $"{Prefix}:Newsfeed";
    public static string NewsfeedPattern => $"{Prefix}:Newsfeed:*";

    // Vote cache keys
    public static string Vote(Guid positionId, Guid userId) => $"{Prefix}:Vote:{positionId}:{userId}";
    public static string VotesByPosition(Guid positionId) => $"{Prefix}:Vote:Position:{positionId}";
    public static string VotePattern => $"{Prefix}:Vote:*";
}


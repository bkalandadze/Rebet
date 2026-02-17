namespace Rebet.Application.DTOs;

public class TicketDetailDto
{
    public Guid Id { get; set; }
    public ExpertDetailDto Expert { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Type { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal TotalOdds { get; set; }
    public decimal Stake { get; set; }
    public decimal PotentialReturn { get; set; }
    public string Visibility { get; set; } = null!;
    public string? Result { get; set; }
    public decimal? FinalOdds { get; set; }
    public string? SettlementNotes { get; set; }
    public int ViewCount { get; set; }
    public int FollowerCount { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? SettledAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<TicketEntryDetailDto> Entries { get; set; } = new();
    public List<CommentDto> Comments { get; set; } = new();
    public string? UserVote { get; set; } // "upvote", "downvote", or null
    public bool? IsFollowing { get; set; } // null if not authenticated
}

public class ExpertDetailDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
    public string? Tier { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public ExpertStatisticsDto? Statistics { get; set; }
    public int SubscriberCount { get; set; }
}

public class ExpertStatisticsDto
{
    public int TotalPositions { get; set; }
    public int TotalTickets { get; set; }
    public int WonPositions { get; set; }
    public int LostPositions { get; set; }
    public decimal WinRate { get; set; }
    public decimal ROI { get; set; }
    public decimal AverageOdds { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestWinStreak { get; set; }
    public decimal Last7DaysWinRate { get; set; }
    public decimal Last30DaysWinRate { get; set; }
    public decimal Last90DaysWinRate { get; set; }
}

public class TicketEntryDetailDto
{
    public Guid Id { get; set; }
    public Guid SportEventId { get; set; }
    public SportEventBasicDto SportEvent { get; set; } = null!;
    public string Market { get; set; } = null!;
    public string Selection { get; set; } = null!;
    public decimal Odds { get; set; }
    public string? Handicap { get; set; }
    public string? Analysis { get; set; }
    public string Status { get; set; } = null!;
    public string? Result { get; set; }
    public string? ResultNotes { get; set; }
    public DateTime EventStartTime { get; set; }
    public DateTime? SettledAt { get; set; }
    public int DisplayOrder { get; set; }
}

public class SportEventBasicDto
{
    public Guid Id { get; set; }
    public string Sport { get; set; } = null!;
    public string? League { get; set; }
    public string HomeTeam { get; set; } = null!;
    public string AwayTeam { get; set; } = null!;
    public string? HomeTeamLogo { get; set; }
    public string? AwayTeamLogo { get; set; }
    public DateTime StartTime { get; set; }
    public string Status { get; set; } = null!;
}

public class CommentDto
{
    public Guid Id { get; set; }
    public UserBasicDto User { get; set; } = null!;
    public string Content { get; set; } = null!;
    public Guid? ParentCommentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CommentDto> Replies { get; set; } = new();
}

public class UserBasicDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? Avatar { get; set; }
}


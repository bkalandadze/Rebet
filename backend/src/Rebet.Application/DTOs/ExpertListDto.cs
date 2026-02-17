namespace Rebet.Application.DTOs;

public class ExpertListDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
    public string Tier { get; set; } = null!;
    public bool IsVerified { get; set; }
    public ExpertStatisticsListDto Statistics { get; set; } = null!;
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int SubscriberCount { get; set; }
    public string? UserVote { get; set; } // "upvote", "downvote", or null
    public bool? IsSubscribed { get; set; } // null if not authenticated
}

public class ExpertStatisticsListDto
{
    public int TotalPositions { get; set; }
    public int TotalTickets { get; set; }
    public decimal WinRate { get; set; }
    public decimal ROI { get; set; }
    public int CurrentStreak { get; set; }
    public decimal Last30DaysWinRate { get; set; }
    public decimal AverageOdds { get; set; }
}


namespace Rebet.Application.DTOs;

public class TicketListDto
{
    public Guid Id { get; set; }
    public ExpertInfoDto Expert { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Type { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal TotalOdds { get; set; }
    public decimal PotentialReturn { get; set; }
    public int EntryCount { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int CommentCount { get; set; }
    public int ViewCount { get; set; }
    public int FollowerCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? UserVote { get; set; } // "upvote", "downvote", or null
    public bool? IsFollowing { get; set; } // null if not authenticated
}

public class ExpertInfoDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? Avatar { get; set; }
    public decimal? WinRate { get; set; }
    public string? Tier { get; set; }
}


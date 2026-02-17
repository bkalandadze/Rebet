namespace Rebet.Application.DTOs;

public class PositionListDto
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public string CreatorName { get; set; } = null!;
    public string? CreatorAvatar { get; set; }
    public bool IsExpert { get; set; }
    public SportEventListDto SportEvent { get; set; } = null!;
    public string Market { get; set; } = null!;
    public string Selection { get; set; } = null!;
    public decimal Odds { get; set; }
    public string? Analysis { get; set; }
    public string Status { get; set; } = null!;
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int VoterCount { get; set; }
    public decimal PredictionPercentage { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UserVote { get; set; } // "upvote", "downvote", or null
}

public class SportEventListDto
{
    public Guid Id { get; set; }
    public string HomeTeam { get; set; } = null!;
    public string AwayTeam { get; set; } = null!;
    public string League { get; set; } = null!;
    public DateTime StartTime { get; set; }
}


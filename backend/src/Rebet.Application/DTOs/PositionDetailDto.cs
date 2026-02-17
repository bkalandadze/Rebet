namespace Rebet.Application.DTOs;

public class PositionDetailDto
{
    public Guid Id { get; set; }
    public CreatorDetailDto Creator { get; set; } = null!;
    public SportEventDetailDto SportEvent { get; set; } = null!;
    public string Market { get; set; } = null!;
    public string Selection { get; set; } = null!;
    public decimal Odds { get; set; }
    public string? Analysis { get; set; }
    public string Status { get; set; } = null!;
    public string? Result { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int VoterCount { get; set; }
    public decimal PredictionPercentage { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SettledAt { get; set; }
    public string? UserVote { get; set; } // "upvote", "downvote", or null
}

public class CreatorDetailDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? Avatar { get; set; }
    public bool IsExpert { get; set; }
    public decimal? WinRate { get; set; }
    public int? SubscriberCount { get; set; }
}

public class SportEventDetailDto
{
    public Guid Id { get; set; }
    public string HomeTeam { get; set; } = null!;
    public string AwayTeam { get; set; } = null!;
    public string League { get; set; } = null!;
    public string Sport { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public string Status { get; set; } = null!;
    public decimal? HomeWinOdds { get; set; }
    public decimal? DrawOdds { get; set; }
    public decimal? AwayWinOdds { get; set; }
}


namespace Rebet.Application.DTOs;

public class CastVoteResponse
{
    public Guid VoteableId { get; set; }
    public string VoteableType { get; set; } = null!;
    public string? VoteType { get; set; } // "upvote", "downvote", or null if removed
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int? VoterCount { get; set; } // Only for Position
    public decimal? PredictionPercentage { get; set; } // Only for Position
}


namespace Rebet.Application.DTOs;

public class VoteResponse
{
    public Guid PositionId { get; set; }
    public string VoteType { get; set; } = null!; // "upvote", "downvote", or null if removed
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int VoterCount { get; set; }
    public decimal PredictionPercentage { get; set; }
}


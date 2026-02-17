namespace Rebet.Application.DTOs;

public class PositionDto
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public string CreatorType { get; set; } = null!;
    public Guid SportEventId { get; set; }
    public string Market { get; set; } = null!;
    public string Selection { get; set; } = null!;
    public decimal Odds { get; set; }
    public string? Analysis { get; set; }
    public string Status { get; set; } = null!;
    public string? Result { get; set; }
    public int ViewCount { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int VoterCount { get; set; }
    public decimal PredictionPercentage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SettledAt { get; set; }
}


using Rebet.Domain.Enums;

namespace Rebet.Domain.Entities;

public class Position : BaseEntity
{
    public Guid CreatorId { get; set; }
    public UserRole CreatorType { get; set; }
    public Guid SportEventId { get; set; }
    
    // Position Details
    public string Market { get; set; } = null!;
    public string Selection { get; set; } = null!;
    public decimal Odds { get; set; }
    public string? Analysis { get; set; }
    
    // Status
    public PositionStatus Status { get; set; } = PositionStatus.Pending;
    public PositionResult? Result { get; set; }
    
    // Engagement
    public int ViewCount { get; set; } = 0;
    public int UpvoteCount { get; set; } = 0;
    public int DownvoteCount { get; set; } = 0;
    public int VoterCount { get; set; } = 0;
    public decimal PredictionPercentage { get; set; } = 0.00m;
    
    public DateTime? SettledAt { get; set; }
    
    // Navigation Properties
    public User Creator { get; set; } = null!;
    public SportEvent SportEvent { get; set; } = null!;
    
    // Methods
    public void RecalculatePredictionPercentage()
    {
        var totalVotes = UpvoteCount + DownvoteCount;
        PredictionPercentage = totalVotes > 0 
            ? (decimal)UpvoteCount / totalVotes * 100 
            : 0;
    }
    
    public void IncrementViewCount()
    {
        ViewCount++;
    }
}


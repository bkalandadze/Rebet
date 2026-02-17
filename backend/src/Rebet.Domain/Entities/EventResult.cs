namespace Rebet.Domain.Entities;

public class EventResult : BaseEntity
{
    public Guid SportEventId { get; set; }
    
    public string? FinalScore { get; set; }
    public string? Winner { get; set; } // "Home", "Away", "Draw"
    public string? HalfTimeScore { get; set; }
    
    public string? MarketResultsJson { get; set; } // JSONB
    
    public DateTime CompletedAt { get; set; }
    public DateTime SettledAt { get; set; }
    
    // Navigation
    public SportEvent SportEvent { get; set; } = null!;
}


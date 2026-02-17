namespace Rebet.Domain.Entities;

public class EventMarketData : BaseEntity
{
    public Guid SportEventId { get; set; }
    
    public string MarketsJson { get; set; } = null!; // JSONB
    
    public long SnapshotEpoch { get; set; }
    public DateTime SnapshotAt { get; set; }
    
    // Navigation
    public SportEvent SportEvent { get; set; } = null!;
}


using Rebet.Domain.Enums;

namespace Rebet.Domain.Entities;

public class SportEvent : BaseEntity
{
    public string ExternalEventId { get; set; } = null!;
    
    public string Sport { get; set; } = null!;
    public string League { get; set; } = null!;
    public string HomeTeam { get; set; } = null!;
    public string AwayTeam { get; set; } = null!;
    public string? HomeTeamLogo { get; set; }
    public string? AwayTeamLogo { get; set; }
    
    public DateTime StartTimeUtc { get; set; }
    public long StartTimeEpoch { get; set; }
    
    public EventStatus Status { get; set; } = EventStatus.Scheduled;
    
    // Common Odds (denormalized)
    public decimal? HomeWinOdds { get; set; }
    public decimal? DrawOdds { get; set; }
    public decimal? AwayWinOdds { get; set; }
    public decimal? Over25Odds { get; set; }
    public decimal? Under25Odds { get; set; }
    
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
    public bool HasActiveTickets { get; set; } = false;
    
    // Navigation Properties
    public EventMarketData? MarketData { get; set; }
    public EventResult? Result { get; set; }
    public ICollection<Position> Positions { get; set; } = new List<Position>();
    public ICollection<TicketEntry> TicketEntries { get; set; } = new List<TicketEntry>();
}


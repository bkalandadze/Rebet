using Rebet.Domain.Enums;

namespace Rebet.Domain.Entities;

public class TicketEntry : BaseEntity
{
    public Guid TicketId { get; set; }
    public Guid SportEventId { get; set; }
    
    // Event Details (denormalized for history)
    public string Sport { get; set; } = null!;
    public string? League { get; set; }
    public string HomeTeam { get; set; } = null!;
    public string AwayTeam { get; set; } = null!;
    public DateTime EventStartTime { get; set; }
    
    // Betting Details
    public string Market { get; set; } = null!;
    public string Selection { get; set; } = null!;
    public decimal Odds { get; set; }
    public string? Handicap { get; set; }
    
    // Result
    public EntryStatus Status { get; set; } = EntryStatus.Pending;
    public EntryResult? Result { get; set; }
    public string? ResultNotes { get; set; }
    public DateTime? SettledAt { get; set; }
    
    public string? Analysis { get; set; }
    public int DisplayOrder { get; set; } = 0;
    
    // Navigation Properties
    public Ticket Ticket { get; set; } = null!;
    public SportEvent SportEvent { get; set; } = null!;
}


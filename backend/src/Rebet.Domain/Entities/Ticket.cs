using Rebet.Domain.Enums;
using System.Linq;

namespace Rebet.Domain.Entities;

public class Ticket : BaseEntity
{
    public Guid ExpertId { get; set; }
    
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public TicketType Type { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Draft;
    
    public decimal TotalOdds { get; set; }
    public decimal Stake { get; set; }
    public decimal PotentialReturn { get; set; }
    
    public TicketVisibility Visibility { get; set; } = TicketVisibility.Public;
    
    // Result
    public TicketResult? Result { get; set; }
    public decimal? FinalOdds { get; set; }
    public string? SettlementNotes { get; set; }
    
    // Engagement
    public int ViewCount { get; set; } = 0;
    public int FollowerCount { get; set; } = 0;
    public int UpvoteCount { get; set; } = 0;
    public int DownvoteCount { get; set; } = 0;
    public int CommentCount { get; set; } = 0;
    
    public DateTime? PublishedAt { get; set; }
    public DateTime? SettledAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    
    // Navigation Properties
    public ICollection<TicketEntry> Entries { get; set; } = new List<TicketEntry>();
    
    // Methods
    public void CalculateTotalOdds()
    {
        TotalOdds = Entries.Aggregate(1.0m, (acc, entry) => acc * entry.Odds);
        PotentialReturn = Stake * TotalOdds;
    }
    
    public void Publish()
    {
        Status = TicketStatus.Active;
        PublishedAt = DateTime.UtcNow;
        ExpiresAt = Entries.Max(e => e.EventStartTime);
    }
}


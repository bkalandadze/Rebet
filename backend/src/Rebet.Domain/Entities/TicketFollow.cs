namespace Rebet.Domain.Entities;

public class TicketFollow : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid TicketId { get; set; }
    
    public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public User User { get; set; } = null!;
    public Ticket Ticket { get; set; } = null!;
}


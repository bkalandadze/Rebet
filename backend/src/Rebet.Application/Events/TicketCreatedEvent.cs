using MediatR;

namespace Rebet.Application.Events;

public class TicketCreatedEvent : INotification
{
    public Guid TicketId { get; set; }
    public Guid ExpertId { get; set; }
    public string Title { get; set; } = null!;
    public string Type { get; set; } = null!;
    public decimal TotalOdds { get; set; }
    public decimal Stake { get; set; }
    public int EntryCount { get; set; }
    public DateTime CreatedAt { get; set; }
}


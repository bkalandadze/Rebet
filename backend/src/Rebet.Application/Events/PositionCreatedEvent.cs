using MediatR;

namespace Rebet.Application.Events;

public class PositionCreatedEvent : INotification
{
    public Guid PositionId { get; set; }
    public Guid CreatorId { get; set; }
    public string CreatorType { get; set; } = null!;
    public Guid SportEventId { get; set; }
    public string Market { get; set; } = null!;
    public string Selection { get; set; } = null!;
    public decimal Odds { get; set; }
    public DateTime CreatedAt { get; set; }
}


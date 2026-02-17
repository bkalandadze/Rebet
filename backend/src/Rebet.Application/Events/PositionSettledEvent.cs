using Rebet.Domain.Enums;
using MediatR;

namespace Rebet.Application.Events;

public class PositionSettledEvent : INotification
{
    public Guid PositionId { get; set; }
    public Guid CreatorId { get; set; }
    public UserRole CreatorType { get; set; }
    public Guid? ExpertId { get; set; }
    public PositionResult Result { get; set; }
    public decimal Odds { get; set; }
    public string Market { get; set; } = null!;
    public string Selection { get; set; } = null!;
    public DateTime SettledAt { get; set; }
}


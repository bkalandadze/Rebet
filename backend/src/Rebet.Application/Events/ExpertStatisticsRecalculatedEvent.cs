using MediatR;

namespace Rebet.Application.Events;

public class ExpertStatisticsRecalculatedEvent : INotification
{
    public Guid ExpertId { get; set; }
    public int? PreviousStreak { get; set; }
    public int CurrentStreak { get; set; }
    public int? PreviousRank { get; set; }
    public int? CurrentRank { get; set; }
    public DateTime RecalculatedAt { get; set; }
}


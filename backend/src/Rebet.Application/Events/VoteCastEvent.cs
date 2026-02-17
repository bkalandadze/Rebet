using Rebet.Domain.Enums;
using MediatR;

namespace Rebet.Application.Events;

public class VoteCastEvent : INotification
{
    public Guid VoteableId { get; set; }
    public VoteableType VoteableType { get; set; }
    public Guid UserId { get; set; }
    public VoteType? VoteType { get; set; } // null if vote was removed
    public DateTime CastAt { get; set; }
}


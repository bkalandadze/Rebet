using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Commands.Vote;

public class CastVoteCommand : IRequest<CastVoteResponse>
{
    public Guid VoteableId { get; set; }
    public int VoteableType { get; set; } // 1=Position, 2=Ticket, 3=Expert
    public int VoteType { get; set; } // 1=Upvote, 2=Downvote
    public Guid UserId { get; set; } // Will be set from authenticated user context
}


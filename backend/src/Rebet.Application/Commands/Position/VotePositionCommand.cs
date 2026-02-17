using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Commands.Position;

public class VotePositionCommand : IRequest<VoteResponse>
{
    public Guid PositionId { get; set; }
    public int VoteType { get; set; } // 1 = Upvote, 2 = Downvote
    public Guid UserId { get; set; } // Will be set from authenticated user context
}


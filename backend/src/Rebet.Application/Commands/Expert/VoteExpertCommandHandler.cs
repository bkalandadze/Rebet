using Rebet.Application.Commands.Vote;
using Rebet.Application.Interfaces;
using MediatR;

namespace Rebet.Application.Commands.Expert;

public class VoteExpertCommandHandler : IRequestHandler<VoteExpertCommand, VoteExpertResponse>
{
    private readonly IMediator _mediator;

    public VoteExpertCommandHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<VoteExpertResponse> Handle(VoteExpertCommand request, CancellationToken cancellationToken)
    {
        // Use the existing CastVoteCommand for experts
        var castVoteCommand = new CastVoteCommand
        {
            VoteableId = request.ExpertId,
            VoteableType = 3, // Expert
            VoteType = request.VoteType,
            UserId = request.UserId
        };

        var result = await _mediator.Send(castVoteCommand, cancellationToken);

        return new VoteExpertResponse
        {
            ExpertId = request.ExpertId,
            VoteType = result.VoteType,
            UpvoteCount = result.UpvoteCount,
            DownvoteCount = result.DownvoteCount
        };
    }
}


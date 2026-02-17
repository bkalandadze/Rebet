using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Commands.Expert;

public class VoteExpertCommand : IRequest<VoteExpertResponse>
{
    public Guid ExpertId { get; set; }
    public int VoteType { get; set; } // 1=Upvote, 2=Downvote
    public Guid UserId { get; set; }
}

public class VoteExpertResponse
{
    public Guid ExpertId { get; set; }
    public string? VoteType { get; set; } // "upvote", "downvote", or null
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
}


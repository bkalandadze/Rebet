using MediatR;

namespace Rebet.Application.Commands.Ticket;

public class FollowTicketCommand : IRequest<FollowTicketResponse>
{
    public Guid TicketId { get; set; }
    public Guid UserId { get; set; }
}

public class FollowTicketResponse
{
    public Guid TicketId { get; set; }
    public bool IsFollowing { get; set; }
    public int FollowerCount { get; set; }
}


using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Commands.Ticket;

public class AddCommentCommand : IRequest<CommentDto>
{
    public Guid TicketId { get; set; }
    public string Content { get; set; } = null!;
    public Guid? ParentCommentId { get; set; }
    public Guid UserId { get; set; }
}


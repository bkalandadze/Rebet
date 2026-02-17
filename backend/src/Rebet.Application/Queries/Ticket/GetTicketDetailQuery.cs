using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Queries.Ticket;

public class GetTicketDetailQuery : IRequest<TicketDetailDto>
{
    public Guid TicketId { get; set; }
    public Guid? UserId { get; set; } // Optional - current user ID for vote status, follow status, and blurring logic
}


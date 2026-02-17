using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using Rebet.Domain.Enums;
using MediatR;

namespace Rebet.Application.Queries.Ticket;

public class GetTopTicketsQueryHandler : IRequestHandler<GetTopTicketsQuery, PagedResult<TicketListDto>>
{
    private readonly ITicketRepository _ticketRepository;

    public GetTopTicketsQueryHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<PagedResult<TicketListDto>> Handle(GetTopTicketsQuery request, CancellationToken cancellationToken)
    {
        // Map type string to UserRole enum
        UserRole creatorType = request.Type.ToLower() switch
        {
            "expert" => UserRole.Expert,
            "user" => UserRole.User,
            _ => throw new ArgumentException($"Invalid type: {request.Type}. Must be 'expert' or 'user'.")
        };

        // Map status string to TicketStatus enum if provided
        TicketStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            status = request.Status.ToLower() switch
            {
                "active" => TicketStatus.Active,
                "settled" => TicketStatus.Settled,
                "draft" => TicketStatus.Draft,
                "void" => TicketStatus.Void,
                "expired" => TicketStatus.Expired,
                _ => throw new ArgumentException($"Invalid status: {request.Status}. Must be 'active', 'settled', 'draft', 'void', or 'expired'.")
            };
        }

        return await _ticketRepository.GetTopTicketsAsync(
            creatorType,
            request.Sport,
            status,
            request.MinOdds,
            request.SortBy,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}


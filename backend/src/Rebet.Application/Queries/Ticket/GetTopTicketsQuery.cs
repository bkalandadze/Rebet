using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Queries.Ticket;

public class GetTopTicketsQuery : IRequest<PagedResult<TicketListDto>>
{
    public string Type { get; set; } = null!; // "expert" or "user" (required)
    public string? Sport { get; set; } // Optional filter by sport
    public string? Status { get; set; } // Optional filter by status: "active", "settled"
    public decimal? MinOdds { get; set; } // Optional minimum odds filter
    public string SortBy { get; set; } = "odds"; // "odds", "upvotes", "created"
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}


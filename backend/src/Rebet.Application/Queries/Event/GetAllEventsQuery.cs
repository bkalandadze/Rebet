using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Queries.Event;

public class GetAllEventsQuery : IRequest<PagedResult<EventListDto>>
{
    public string? Sport { get; set; }
    public string? League { get; set; }
    public string? Date { get; set; } // Format: "yyyy-MM-dd"
    public string? Status { get; set; } // "scheduled", "live", "finished"
    public bool? HasExpertPredictions { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}


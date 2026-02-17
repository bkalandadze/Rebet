using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using Rebet.Domain.Enums;
using MediatR;

namespace Rebet.Application.Queries.Event;

public class GetAllEventsQueryHandler : IRequestHandler<GetAllEventsQuery, PagedResult<EventListDto>>
{
    private readonly ISportEventRepository _sportEventRepository;

    public GetAllEventsQueryHandler(ISportEventRepository sportEventRepository)
    {
        _sportEventRepository = sportEventRepository;
    }

    public async Task<PagedResult<EventListDto>> Handle(GetAllEventsQuery request, CancellationToken cancellationToken)
    {
        // Parse date if provided
        DateTime? date = null;
        if (!string.IsNullOrWhiteSpace(request.Date) && DateTime.TryParse(request.Date, out var parsedDate))
        {
            date = parsedDate.Date;
        }

        // Parse status if provided
        EventStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            status = request.Status.ToLower() switch
            {
                "scheduled" => EventStatus.Scheduled,
                "live" => EventStatus.Live,
                "finished" => EventStatus.Finished,
                "cancelled" => EventStatus.Cancelled,
                _ => null
            };
        }

        return await _sportEventRepository.GetAllEventsAsync(
            request.Sport,
            request.League,
            date,
            status,
            request.HasExpertPredictions,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}


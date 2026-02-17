using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using MediatR;

namespace Rebet.Application.Queries.Event;

public class GetEventDetailQueryHandler : IRequestHandler<GetEventDetailQuery, EventDetailDto>
{
    private readonly ISportEventRepository _sportEventRepository;

    public GetEventDetailQueryHandler(ISportEventRepository sportEventRepository)
    {
        _sportEventRepository = sportEventRepository;
    }

    public async Task<EventDetailDto> Handle(GetEventDetailQuery request, CancellationToken cancellationToken)
    {
        var eventDetail = await _sportEventRepository.GetEventDetailAsync(request.EventId, cancellationToken);
        
        if (eventDetail == null)
        {
            throw new KeyNotFoundException($"Event with ID {request.EventId} not found");
        }

        return eventDetail;
    }
}


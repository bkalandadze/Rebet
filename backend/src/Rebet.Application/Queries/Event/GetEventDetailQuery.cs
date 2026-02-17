using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Queries.Event;

public class GetEventDetailQuery : IRequest<EventDetailDto>
{
    public Guid EventId { get; set; }
}


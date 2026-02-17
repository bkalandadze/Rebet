using Rebet.Application.DTOs;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;

namespace Rebet.Application.Interfaces;

public interface ISportEventRepository : IRepository<SportEvent>
{
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsScheduledAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<EventListDto>> GetAllEventsAsync(
        string? sport,
        string? league,
        DateTime? date,
        EventStatus? status,
        bool? hasExpertPredictions,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<EventDetailDto?> GetEventDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EventDetailDto?> GetTopGameOfDayAsync(string? sport, DateTime? date, CancellationToken cancellationToken = default);
}


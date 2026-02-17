using Rebet.Application.DTOs;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;

namespace Rebet.Application.Interfaces;

public interface IPositionRepository : IRepository<Position>
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<PositionListDto>> GetTopPositionsAsync(
        UserRole creatorType,
        string? sport,
        PositionStatus? status,
        string sortBy,
        int page,
        int pageSize,
        Guid? userId = null,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Position>> GetByCreatorIdAsync(
        Guid creatorId,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Position>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
    Task<PagedResult<Position>> GetWonPositionsAsync(
        string? sport,
        DateTime? startDate,
        decimal? minOdds,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}


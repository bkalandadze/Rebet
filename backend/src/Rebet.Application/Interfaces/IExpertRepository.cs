using Rebet.Application.DTOs;
using Rebet.Domain.Entities;

namespace Rebet.Application.Interfaces;

public interface IExpertRepository : IRepository<Expert>
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> DisplayNameExistsAsync(string displayName, CancellationToken cancellationToken = default);
    Task<Expert?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResult<ExpertListDto>> GetLeaderboardAsync(
        string sortBy,
        string? specialization,
        decimal? minWinRate,
        int? tier,
        int page,
        int pageSize,
        Guid? userId = null, // For user-specific data (vote, subscription status)
        CancellationToken cancellationToken = default);
    Task<PagedResult<ExpertListDto>> SearchAsync(
        string searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}


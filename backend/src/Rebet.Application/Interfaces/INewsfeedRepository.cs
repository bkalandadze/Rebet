using Rebet.Application.DTOs;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;

namespace Rebet.Application.Interfaces;

public interface INewsfeedRepository : IRepository<NewsfeedItem>
{
    Task<PagedResult<NewsfeedItemDto>> GetNewsfeedAsync(
        string type,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}


using Rebet.Application.DTOs;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;

namespace Rebet.Application.Interfaces;

public interface ITicketRepository : IRepository<Ticket>
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task<TicketEntry> AddEntryAsync(TicketEntry entry, CancellationToken cancellationToken = default);
    Task<Ticket?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<TicketListDto>> GetTopTicketsAsync(
        UserRole creatorType,
        string? sport,
        TicketStatus? status,
        decimal? minOdds,
        string sortBy,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Ticket>> GetByExpertIdAsync(
        Guid expertId,
        CancellationToken cancellationToken = default);
    Task<int> GetTicketCountForEventAsync(
        Guid sportEventId,
        CancellationToken cancellationToken = default);
}


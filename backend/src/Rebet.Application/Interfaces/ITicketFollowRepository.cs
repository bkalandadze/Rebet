using Rebet.Domain.Entities;

namespace Rebet.Application.Interfaces;

public interface ITicketFollowRepository : IRepository<TicketFollow>
{
    Task<TicketFollow?> GetByUserAndTicketAsync(
        Guid userId,
        Guid ticketId,
        CancellationToken cancellationToken = default);
    
    Task<int> GetFollowerCountAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}


using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Rebet.Infrastructure.Repositories;

public class TicketFollowRepository : Repository<TicketFollow>, ITicketFollowRepository
{
    public TicketFollowRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<TicketFollow?> GetByUserAndTicketAsync(
        Guid userId,
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(
                tf => tf.UserId == userId 
                     && tf.TicketId == ticketId 
                     && !tf.IsDeleted,
                cancellationToken);
    }

    public async Task<int> GetFollowerCountAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(
                tf => tf.TicketId == ticketId 
                     && !tf.IsDeleted,
                cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}


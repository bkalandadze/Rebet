using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Rebet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Rebet.Infrastructure.Repositories;

public class SubscriptionRepository : Repository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Subscription?> GetByUserAndExpertAsync(Guid userId, Guid expertId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(
                s => s.UserId == userId 
                     && s.ExpertId == expertId 
                     && !s.IsDeleted,
                cancellationToken);
    }

    public async Task<int> GetActiveSubscriberCountAsync(Guid expertId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .CountAsync(
                s => s.ExpertId == expertId 
                     && s.Status == SubscriptionStatus.Active 
                     && !s.IsDeleted,
                cancellationToken);
    }
}


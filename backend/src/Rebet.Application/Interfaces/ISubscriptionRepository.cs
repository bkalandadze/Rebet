using Rebet.Domain.Entities;

namespace Rebet.Application.Interfaces;

public interface ISubscriptionRepository : IRepository<Subscription>
{
    Task<Subscription?> GetByUserAndExpertAsync(Guid userId, Guid expertId, CancellationToken cancellationToken = default);
    Task<int> GetActiveSubscriberCountAsync(Guid expertId, CancellationToken cancellationToken = default);
}


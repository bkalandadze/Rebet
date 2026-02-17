using Rebet.Domain.Entities;
using Rebet.Domain.Enums;

namespace Rebet.Application.Interfaces;

public interface IVoteRepository : IRepository<Vote>
{
    Task<Vote?> GetVoteAsync(
        Guid userId,
        VoteableType voteableType,
        Guid voteableId,
        CancellationToken cancellationToken = default);
    
    Task RemoveAsync(Vote vote, CancellationToken cancellationToken = default);
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}


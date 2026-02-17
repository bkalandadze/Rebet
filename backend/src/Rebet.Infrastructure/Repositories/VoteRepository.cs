using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Rebet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Rebet.Infrastructure.Repositories;

public class VoteRepository : Repository<Vote>, IVoteRepository
{
    public VoteRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Vote?> GetVoteAsync(
        Guid userId,
        VoteableType voteableType,
        Guid voteableId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.UserId == userId 
                     && v.VoteableType == voteableType 
                     && v.VoteableId == voteableId
                     && !v.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task RemoveAsync(Vote vote, CancellationToken cancellationToken = default)
    {
        vote.IsDeleted = true;
        _dbSet.Update(vote);
        await Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await transaction.RollbackAsync(cancellationToken);
        }
    }
}


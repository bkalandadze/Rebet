namespace Rebet.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IExpertRepository Experts { get; }
    IPositionRepository Positions { get; }
    ITicketRepository Tickets { get; }
    ISportEventRepository SportEvents { get; }
    IVoteRepository Votes { get; }
    INewsfeedRepository Newsfeed { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}


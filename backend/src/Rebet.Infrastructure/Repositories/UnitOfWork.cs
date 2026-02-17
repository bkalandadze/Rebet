using Rebet.Application.Interfaces;
using Rebet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace Rebet.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    private IUserRepository? _users;
    private IExpertRepository? _experts;
    private IPositionRepository? _positions;
    private ITicketRepository? _tickets;
    private ISportEventRepository? _sportEvents;
    private IVoteRepository? _votes;
    private INewsfeedRepository? _newsfeed;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users
    {
        get
        {
            _users ??= new UserRepository(_context);
            return _users;
        }
    }

    public IExpertRepository Experts
    {
        get
        {
            _experts ??= new ExpertRepository(_context);
            return _experts;
        }
    }

    public IPositionRepository Positions
    {
        get
        {
            _positions ??= new PositionRepository(_context);
            return _positions;
        }
    }

    public ITicketRepository Tickets
    {
        get
        {
            _tickets ??= new TicketRepository(_context);
            return _tickets;
        }
    }

    public ISportEventRepository SportEvents
    {
        get
        {
            _sportEvents ??= new SportEventRepository(_context);
            return _sportEvents;
        }
    }

    public IVoteRepository Votes
    {
        get
        {
            _votes ??= new VoteRepository(_context);
            return _votes;
        }
    }

    public INewsfeedRepository Newsfeed
    {
        get
        {
            _newsfeed ??= new NewsfeedRepository(_context);
            return _newsfeed;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}


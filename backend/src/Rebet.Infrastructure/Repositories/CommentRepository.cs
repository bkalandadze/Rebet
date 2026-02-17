using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Rebet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Rebet.Infrastructure.Repositories;

public class CommentRepository : Repository<Comment>, ICommentRepository
{
    public CommentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Comment?> GetByIdAndTypeAsync(Guid id, CommentableType type, Guid commentableId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(
                c => c.Id == id 
                     && c.CommentableType == type 
                     && c.CommentableId == commentableId 
                     && !c.IsDeleted,
                cancellationToken);
    }

    public async Task<List<Comment>> GetCommentsByTicketAsync(Guid ticketId, int limit, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.CommentableType == CommentableType.Ticket 
                     && c.CommentableId == ticketId 
                     && !c.IsDeleted)
            .Include(c => c.User)
                .ThenInclude(u => u.Profile)
            .Include(c => c.Replies)
                .ThenInclude(r => r.User)
                    .ThenInclude(u => u.Profile)
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}


using Rebet.Domain.Entities;
using Rebet.Domain.Enums;

namespace Rebet.Application.Interfaces;

public interface ICommentRepository : IRepository<Comment>
{
    Task<Comment?> GetByIdAndTypeAsync(Guid id, CommentableType type, Guid commentableId, CancellationToken cancellationToken = default);
    Task<List<Comment>> GetCommentsByTicketAsync(Guid ticketId, int limit, CancellationToken cancellationToken = default);
}


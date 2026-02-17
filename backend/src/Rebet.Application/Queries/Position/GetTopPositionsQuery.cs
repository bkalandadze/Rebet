using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Queries.Position;

public class GetTopPositionsQuery : IRequest<PagedResult<PositionListDto>>
{
    public string Type { get; set; } = null!; // "expert" or "user" (required)
    public string? Sport { get; set; } // Optional filter by sport
    public string? Status { get; set; } // Optional filter by status: "pending", "won", "lost"
    public string SortBy { get; set; } = "upvotes"; // "upvotes", "created", "odds"
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? UserId { get; set; } // Optional - current user ID for vote status
}


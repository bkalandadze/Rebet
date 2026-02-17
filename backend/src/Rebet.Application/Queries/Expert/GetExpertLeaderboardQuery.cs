using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Queries.Expert;

public class GetExpertLeaderboardQuery : IRequest<PagedResult<ExpertListDto>>
{
    public string SortBy { get; set; } = "winRate"; // "winRate", "roi", "upvotes", "subscribers"
    public string? Specialization { get; set; } // Optional filter by specialization
    public decimal? MinWinRate { get; set; } // Optional minimum win rate filter
    public int? Tier { get; set; } // Optional filter by tier (1-5: Bronze to Diamond)
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}


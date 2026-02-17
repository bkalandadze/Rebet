using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using MediatR;

namespace Rebet.Application.Queries.Expert;

public class GetExpertLeaderboardQueryHandler : IRequestHandler<GetExpertLeaderboardQuery, PagedResult<ExpertListDto>>
{
    private readonly IExpertRepository _expertRepository;

    public GetExpertLeaderboardQueryHandler(IExpertRepository expertRepository)
    {
        _expertRepository = expertRepository;
    }

    public async Task<PagedResult<ExpertListDto>> Handle(GetExpertLeaderboardQuery request, CancellationToken cancellationToken)
    {
        // Validate sortBy parameter
        var validSortBy = new[] { "winrate", "roi", "upvotes", "subscribers" };
        if (!validSortBy.Contains(request.SortBy.ToLower()))
        {
            throw new ArgumentException(
                $"Invalid sortBy: {request.SortBy}. Must be one of: {string.Join(", ", validSortBy)}",
                nameof(request.SortBy));
        }

        // Get current user ID if authenticated (for user-specific data like votes and subscriptions)
        // TODO: Extract from HttpContext or pass via request when authentication is implemented
        Guid? userId = null;

        return await _expertRepository.GetLeaderboardAsync(
            request.SortBy,
            request.Specialization,
            request.MinWinRate,
            request.Tier,
            request.Page,
            request.PageSize,
            userId,
            cancellationToken);
    }
}


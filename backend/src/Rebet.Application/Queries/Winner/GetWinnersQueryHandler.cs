using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using MediatR;

namespace Rebet.Application.Queries.Winner;

public class GetWinnersQueryHandler : IRequestHandler<GetWinnersQuery, PagedResult<WinnerDto>>
{
    private readonly IPositionRepository _positionRepository;
    private readonly IExpertRepository _expertRepository;

    public GetWinnersQueryHandler(
        IPositionRepository positionRepository,
        IExpertRepository expertRepository)
    {
        _positionRepository = positionRepository;
        _expertRepository = expertRepository;
    }

    public async Task<PagedResult<WinnerDto>> Handle(GetWinnersQuery request, CancellationToken cancellationToken)
    {
        // Calculate date range based on period
        DateTime? startDate = null;
        if (!string.IsNullOrWhiteSpace(request.Period))
        {
            startDate = request.Period.ToLower() switch
            {
                "today" => DateTime.UtcNow.Date,
                "week" => DateTime.UtcNow.Date.AddDays(-7),
                "month" => DateTime.UtcNow.Date.AddDays(-30),
                _ => null
            };
        }

        // Get won positions using repository method
        var positionsResult = await _positionRepository.GetWonPositionsAsync(
            request.Sport,
            startDate,
            request.MinOdds,
            request.Page,
            request.PageSize,
            cancellationToken);

        var positions = positionsResult.Data.ToList();

        // Get expert IDs
        var expertUserIds = positions.Select(p => p.CreatorId).Distinct().ToList();
        
        // Get experts - need to access through repository
        // For now, we'll get them individually or use a different approach
        var experts = new List<Rebet.Domain.Entities.Expert>();
        foreach (var userId in expertUserIds)
        {
            var expert = await _expertRepository.GetByUserIdAsync(userId, cancellationToken);
            if (expert != null)
            {
                experts.Add(expert);
            }
        }

        var expertDict = experts.ToDictionary(ex => ex.UserId, ex => ex);

        // Map to DTOs
        var winnerDtos = positions.Select(p =>
        {
            var expert = expertDict.ContainsKey(p.CreatorId) ? expertDict[p.CreatorId] : null;
            var stats = expert?.Statistics;

            // Calculate profit percentage (simplified)
            var profitPercentage = p.Odds > 0 ? ((p.Odds - 1) * 100) : 0;
            var profit = $"+{profitPercentage:F0}%";

            return new WinnerDto
            {
                Expert = new ExpertWinnerDto
                {
                    DisplayName = expert?.DisplayName ?? p.Creator.Profile?.DisplayName ?? p.Creator.Email,
                    Avatar = p.Creator.Profile?.Avatar
                },
                SubscriberCount = stats?.TotalSubscribers ?? 0,
                TotalPredictions = stats?.TotalPositions ?? 0,
                Position = new WinnerPositionDto
                {
                    Id = p.Id,
                    Event = $"{p.SportEvent.HomeTeam} vs {p.SportEvent.AwayTeam}",
                    Selection = p.Selection,
                    Odds = p.Odds,
                    Result = "Won"
                },
                Profit = profit,
                WonAt = p.SettledAt ?? p.CreatedAt
            };
        }).ToList();

        return new PagedResult<WinnerDto>
        {
            Data = winnerDtos,
            Pagination = new PagedResult<WinnerDto>.PaginationMetadata
            {
                Page = positionsResult.Pagination.Page,
                PageSize = positionsResult.Pagination.PageSize,
                TotalItems = positionsResult.Pagination.TotalItems,
                TotalPages = positionsResult.Pagination.TotalPages
            }
        };
    }
}


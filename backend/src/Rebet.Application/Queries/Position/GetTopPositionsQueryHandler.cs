using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using Rebet.Domain.Enums;
using MediatR;

namespace Rebet.Application.Queries.Position;

public class GetTopPositionsQueryHandler : IRequestHandler<GetTopPositionsQuery, PagedResult<PositionListDto>>
{
    private readonly IPositionRepository _positionRepository;

    public GetTopPositionsQueryHandler(IPositionRepository positionRepository)
    {
        _positionRepository = positionRepository;
    }

    public async Task<PagedResult<PositionListDto>> Handle(GetTopPositionsQuery request, CancellationToken cancellationToken)
    {
        // Map type string to UserRole enum
        UserRole creatorType = request.Type.ToLower() switch
        {
            "expert" => UserRole.Expert,
            "user" => UserRole.User,
            _ => throw new ArgumentException($"Invalid type: {request.Type}. Must be 'expert' or 'user'.")
        };

        // Map status string to PositionStatus enum if provided
        PositionStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            status = request.Status.ToLower() switch
            {
                "pending" => PositionStatus.Pending,
                "won" => PositionStatus.Won,
                "lost" => PositionStatus.Lost,
                _ => throw new ArgumentException($"Invalid status: {request.Status}. Must be 'pending', 'won', or 'lost'.")
            };
        }

        return await _positionRepository.GetTopPositionsAsync(
            creatorType,
            request.Sport,
            status,
            request.SortBy,
            request.Page,
            request.PageSize,
            request.UserId,
            cancellationToken);
    }
}


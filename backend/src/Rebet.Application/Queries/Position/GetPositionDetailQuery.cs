using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Queries.Position;

public class GetPositionDetailQuery : IRequest<PositionDetailDto>
{
    public Guid PositionId { get; set; }
    public Guid? UserId { get; set; } // Optional - current user ID for vote status and blurring logic
}


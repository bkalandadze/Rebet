using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Commands.Position;

public class CreatePositionCommand : IRequest<PositionDto>
{
    public Guid SportEventId { get; set; }
    public string Market { get; set; } = null!;
    public string Selection { get; set; } = null!;
    public decimal Odds { get; set; }
    public string? Analysis { get; set; }
    public Guid CreatorId { get; set; } // Will be set from authenticated user context
}


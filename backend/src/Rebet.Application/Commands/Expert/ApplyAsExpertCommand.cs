using MediatR;

namespace Rebet.Application.Commands.Expert;

public class ApplyAsExpertCommand : IRequest<ApplyAsExpertResponse>
{
    public Guid UserId { get; set; } // Will be set from authenticated user context
    public string DisplayName { get; set; } = null!;
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
}

public class ApplyAsExpertResponse
{
    public Guid ExpertId { get; set; }
    public string Message { get; set; } = null!;
}


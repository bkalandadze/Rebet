using MediatR;

namespace Rebet.Application.Commands.Expert;

public class SubscribeToExpertCommand : IRequest<SubscribeToExpertResponse>
{
    public Guid ExpertId { get; set; }
    public Guid UserId { get; set; }
}

public class SubscribeToExpertResponse
{
    public Guid ExpertId { get; set; }
    public bool IsSubscribed { get; set; }
    public int SubscriberCount { get; set; }
}


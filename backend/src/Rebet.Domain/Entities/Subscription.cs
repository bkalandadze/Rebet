using Rebet.Domain.Enums;

namespace Rebet.Domain.Entities;

public class Subscription : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ExpertId { get; set; }
    
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public bool ReceiveNotifications { get; set; } = true;
    
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UnsubscribedAt { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
    public Expert Expert { get; set; } = null!;
}


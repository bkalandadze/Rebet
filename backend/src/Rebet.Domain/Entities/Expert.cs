using Rebet.Domain.Enums;

namespace Rebet.Domain.Entities;

public class Expert : BaseEntity
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
    
    public ExpertTier Tier { get; set; } = ExpertTier.Bronze;
    public ExpertStatus Status { get; set; } = ExpertStatus.PendingApproval;
    
    public decimal CommissionRate { get; set; } = 0.1000m; // 10%
    
    public bool IsVerified { get; set; } = false;
    public DateTime? VerifiedAt { get; set; }
    
    public int UpvoteCount { get; set; } = 0;
    public int DownvoteCount { get; set; } = 0;
    
    // Navigation Properties
    public User User { get; set; } = null!;
    public ExpertStatistics? Statistics { get; set; }
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<Subscription> Subscribers { get; set; } = new List<Subscription>();
    
    // Methods
    public bool IsBlurred => Statistics?.WinRate >= 80.0m;
}


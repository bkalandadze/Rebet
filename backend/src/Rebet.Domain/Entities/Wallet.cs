using Rebet.Domain.Enums;

namespace Rebet.Domain.Entities;

public class Wallet : BaseEntity
{
    public Guid UserId { get; set; }
    
    public decimal Balance { get; set; } = 0.00m;
    public decimal PendingBalance { get; set; } = 0.00m;
    public string Currency { get; set; } = "USD";
    
    public WalletStatus Status { get; set; } = WalletStatus.Active;
    
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public User User { get; set; } = null!;
}


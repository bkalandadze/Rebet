using Rebet.Domain.Enums;

namespace Rebet.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Country { get; set; }
    public string Currency { get; set; } = "USD";
    
    public UserRole Role { get; set; } = UserRole.User;
    public UserStatus Status { get; set; } = UserStatus.Active;
    
    public bool IsEmailVerified { get; set; } = false;
    public bool IsPhoneVerified { get; set; } = false;
    public bool IsTwoFactorEnabled { get; set; } = false;
    
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation Properties
    public UserProfile? Profile { get; set; }
    public Expert? Expert { get; set; }
    public Wallet? Wallet { get; set; }
    public ICollection<Position> Positions { get; set; } = new List<Position>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}


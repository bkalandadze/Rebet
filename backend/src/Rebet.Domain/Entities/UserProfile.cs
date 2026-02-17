namespace Rebet.Domain.Entities;

public class UserProfile
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
    public string? TimeZone { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    
    public bool ReceiveEmailNotifications { get; set; } = true;
    public bool ReceivePushNotifications { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public User User { get; set; } = null!;
}


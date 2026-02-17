using Rebet.Domain.Enums;

namespace Rebet.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    
    public NotificationType Type { get; set; }
    
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    
    public string? ActionUrl { get; set; }
    
    public bool IsRead { get; set; } = false;
    
    public string? MetadataJson { get; set; } // JSONB
    
    public DateTime? ReadAt { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
}


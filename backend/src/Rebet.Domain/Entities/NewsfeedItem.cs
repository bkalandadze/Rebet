using Rebet.Domain.Enums;

namespace Rebet.Domain.Entities;

public class NewsfeedItem : BaseEntity
{
    public NewsfeedType Type { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? ActionUrl { get; set; }
    
    public Guid? ExpertId { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? TicketId { get; set; }
    
    public string? MetadataJson { get; set; } // JSONB
    
    // Navigation
    public Expert? Expert { get; set; }
    public Position? Position { get; set; }
    public Ticket? Ticket { get; set; }
}


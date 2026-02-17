namespace Rebet.Application.DTOs;

public class NewsfeedItemDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!; // "NewPosition", "SuccessfulPrediction", etc.
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? ActionUrl { get; set; }
    public ExpertBasicDto? Expert { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExpertBasicDto
{
    public string DisplayName { get; set; } = null!;
    public string? Avatar { get; set; }
}


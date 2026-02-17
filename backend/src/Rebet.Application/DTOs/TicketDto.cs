namespace Rebet.Application.DTOs;

public class TicketDto
{
    public Guid Id { get; set; }
    public Guid ExpertId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Type { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal TotalOdds { get; set; }
    public decimal Stake { get; set; }
    public decimal PotentialReturn { get; set; }
    public string Visibility { get; set; } = null!;
    public string? Result { get; set; }
    public decimal? FinalOdds { get; set; }
    public int ViewCount { get; set; }
    public int FollowerCount { get; set; }
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int CommentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? SettledAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<TicketEntryDto> Entries { get; set; } = new();
}


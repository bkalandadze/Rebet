namespace Rebet.Application.DTOs;

public class TicketEntryDto
{
    public Guid SportEventId { get; set; }
    public string Market { get; set; } = null!;
    public string Selection { get; set; } = null!;
    public decimal Odds { get; set; }
    public string? Analysis { get; set; }
}


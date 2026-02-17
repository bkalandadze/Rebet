using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Queries.Event;

public class GetTopGameOfDayQuery : IRequest<TopGameDto>
{
    public string? Sport { get; set; }
    public string? Date { get; set; } // Format: "yyyy-MM-dd"
}

public class TopGameDto
{
    public EventDetailDto Event { get; set; } = null!;
    public string Badge { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public TopGameStatisticsDto Statistics { get; set; } = null!;
    public List<ExpertListDto>? TopExperts { get; set; }
}

public class TopGameStatisticsDto
{
    public int ExpertPositions { get; set; }
    public int UserPositions { get; set; }
    public int TotalTickets { get; set; }
}


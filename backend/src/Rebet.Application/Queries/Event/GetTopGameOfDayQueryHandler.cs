using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using MediatR;

namespace Rebet.Application.Queries.Event;

public class GetTopGameOfDayQueryHandler : IRequestHandler<GetTopGameOfDayQuery, TopGameDto>
{
    private readonly ISportEventRepository _sportEventRepository;
    private readonly ITicketRepository _ticketRepository;

    public GetTopGameOfDayQueryHandler(
        ISportEventRepository sportEventRepository,
        ITicketRepository ticketRepository)
    {
        _sportEventRepository = sportEventRepository;
        _ticketRepository = ticketRepository;
    }

    public async Task<TopGameDto> Handle(GetTopGameOfDayQuery request, CancellationToken cancellationToken)
    {
        // Parse date if provided, otherwise use today
        DateTime date = DateTime.UtcNow.Date;
        if (!string.IsNullOrWhiteSpace(request.Date) && DateTime.TryParse(request.Date, out var parsedDate))
        {
            date = parsedDate.Date;
        }

        var topGame = await _sportEventRepository.GetTopGameOfDayAsync(request.Sport, date, cancellationToken);
        
        if (topGame == null)
        {
            throw new KeyNotFoundException("No top game found for the specified date");
        }

        // Calculate ticket count for this event
        var ticketCount = await _ticketRepository.GetTicketCountForEventAsync(topGame.Id, cancellationToken);

        // Build TopGameDto from EventDetailDto
        // Note: The repository should return the event with statistics
        return new TopGameDto
        {
            Event = topGame,
            Badge = "🔥 Match of the Day",
            Reason = "Most predicted game",
            Statistics = new TopGameStatisticsDto
            {
                ExpertPositions = topGame.Positions?.Count(p => p.IsExpert) ?? 0,
                UserPositions = topGame.Positions?.Count(p => !p.IsExpert) ?? 0,
                TotalTickets = ticketCount
            },
            TopExperts = topGame.TopExperts
        };
    }
}


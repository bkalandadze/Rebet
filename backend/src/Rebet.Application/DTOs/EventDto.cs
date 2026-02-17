namespace Rebet.Application.DTOs;

// Note: SportEventListDto already exists in PositionListDto.cs, but we need an extended version for events
public class EventListDto
{
    public Guid Id { get; set; }
    public string Sport { get; set; } = null!;
    public string League { get; set; } = null!;
    public string HomeTeam { get; set; } = null!;
    public string AwayTeam { get; set; } = null!;
    public string? HomeTeamLogo { get; set; }
    public string? AwayTeamLogo { get; set; }
    public DateTime StartTime { get; set; }
    public string Status { get; set; } = null!;
    public EventOddsDto? Odds { get; set; }
    public EventSentimentDto? Sentiment { get; set; }
    public int PositionCount { get; set; }
    public int TicketCount { get; set; }
}

// Extended event detail DTO for event endpoints (includes more info than SportEventDetailDto in PositionDetailDto)
public class EventDetailDto
{
    public Guid Id { get; set; }
    public string Sport { get; set; } = null!;
    public string League { get; set; } = null!;
    public string HomeTeam { get; set; } = null!;
    public string AwayTeam { get; set; } = null!;
    public string? HomeTeamLogo { get; set; }
    public string? AwayTeamLogo { get; set; }
    public DateTime StartTime { get; set; }
    public string Status { get; set; } = null!;
    public string? Venue { get; set; }
    public EventMarketsDto? Markets { get; set; }
    public EventSentimentDto? Sentiment { get; set; }
    public List<PositionListDto>? Positions { get; set; }
    public List<ExpertListDto>? TopExperts { get; set; }
}

public class EventOddsDto
{
    public decimal? HomeWin { get; set; }
    public decimal? Draw { get; set; }
    public decimal? AwayWin { get; set; }
}

public class EventSentimentDto
{
    public SentimentBreakdownDto? Expert { get; set; }
    public SentimentBreakdownDto? User { get; set; }
}

public class SentimentBreakdownDto
{
    public int HomeWin { get; set; }
    public int Draw { get; set; }
    public int AwayWin { get; set; }
    public int TotalExperts { get; set; }
    public int TotalVotes { get; set; }
}

public class EventMarketsDto
{
    public EventOddsDto? MatchResult { get; set; }
    public List<OverUnderDto>? OverUnder { get; set; }
    public BothTeamsScoreDto? BothTeamsScore { get; set; }
}

public class OverUnderDto
{
    public decimal Line { get; set; }
    public decimal Over { get; set; }
    public decimal Under { get; set; }
}

public class BothTeamsScoreDto
{
    public decimal Yes { get; set; }
    public decimal No { get; set; }
}


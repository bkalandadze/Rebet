namespace Rebet.Application.Interfaces;

public interface IOddsProviderService
{
    Task<OddsApiResponse> GetPrematchOddsAsync(CancellationToken cancellationToken = default);
}

public class OddsApiResponse
{
    public Dictionary<string, OddsEventData> Events { get; set; } = new();
}

public class OddsEventData
{
    public string EventId { get; set; } = null!;
    public string Sport { get; set; } = null!;
    public string League { get; set; } = null!;
    public string HomeTeam { get; set; } = null!;
    public string AwayTeam { get; set; } = null!;
    public string? HomeTeamLogo { get; set; }
    public string? AwayTeamLogo { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public long StartTimeEpoch { get; set; }
    public Dictionary<string, OddsMarket> Markets { get; set; } = new();
}

public class OddsMarket
{
    public string MarketType { get; set; } = null!;
    public List<OddsOutcome> Outcomes { get; set; } = new();
}

public class OddsOutcome
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal Odds { get; set; }
    public string? Handicap { get; set; }
}


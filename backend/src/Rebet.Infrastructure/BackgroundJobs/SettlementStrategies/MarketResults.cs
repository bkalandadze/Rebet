namespace Rebet.Infrastructure.BackgroundJobs.SettlementStrategies;

public class MarketResults
{
    public string? MatchResult { get; set; }
    public int? TotalGoals { get; set; }
    public string? OverUnder25 { get; set; }
    public bool? BothTeamsScore { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
}


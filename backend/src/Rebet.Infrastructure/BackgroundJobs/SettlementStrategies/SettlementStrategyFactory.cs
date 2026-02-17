using Microsoft.Extensions.Logging;

namespace Rebet.Infrastructure.BackgroundJobs.SettlementStrategies;

public class SettlementStrategyFactory
{
    private readonly ILogger _logger;
    private readonly ScoreParser _scoreParser;
    private readonly Dictionary<string, ISettlementStrategy> _strategies;

    public SettlementStrategyFactory(ILogger<SettlePositionsJob> logger)
    {
        _logger = logger;
        _scoreParser = new ScoreParser();
        _strategies = InitializeStrategies();
    }

    public ISettlementStrategy GetStrategy(string market)
    {
        var normalizedMarket = market.ToLowerInvariant();
        
        return normalizedMarket switch
        {
            "match result" or "1x2" or "full time result" => _strategies["match_result"],
            "over/under" or "total goals" or "o/u" => _strategies["over_under"],
            "both teams score" or "btts" => _strategies["both_teams_score"],
            "asian handicap" or "handicap" => _strategies["asian_handicap"],
            _ => _strategies["generic"]
        };
    }

    private Dictionary<string, ISettlementStrategy> InitializeStrategies()
    {
        return new Dictionary<string, ISettlementStrategy>
        {
            { "match_result", new MatchResultSettlementStrategy(_logger) },
            { "over_under", new OverUnderSettlementStrategy(_logger, _scoreParser) },
            { "both_teams_score", new BothTeamsScoreSettlementStrategy(_logger, _scoreParser) },
            { "asian_handicap", new AsianHandicapSettlementStrategy(_logger) },
            { "generic", new GenericMarketSettlementStrategy(_logger) }
        };
    }
}


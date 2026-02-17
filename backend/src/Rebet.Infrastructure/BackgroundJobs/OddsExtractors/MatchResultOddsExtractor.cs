using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;

namespace Rebet.Infrastructure.BackgroundJobs.OddsExtractors;

public class MatchResultOddsExtractor : IOddsExtractor
{
    private static readonly string[] MarketKeys = { "match_result", "1X2" };
    private static readonly Dictionary<string, Action<SportEvent, decimal>> OutcomeMappings = new()
    {
        { "1", (se, odds) => se.HomeWinOdds = odds },
        { "home", (se, odds) => se.HomeWinOdds = odds },
        { "x", (se, odds) => se.DrawOdds = odds },
        { "draw", (se, odds) => se.DrawOdds = odds },
        { "2", (se, odds) => se.AwayWinOdds = odds },
        { "away", (se, odds) => se.AwayWinOdds = odds }
    };

    public void ExtractOdds(SportEvent sportEvent, Dictionary<string, OddsMarket> markets)
    {
        var market = FindMarket(markets, MarketKeys);
        if (market == null)
            return;

        foreach (var outcome in market.Outcomes)
        {
            var outcomeId = outcome.Id.ToLower();
            if (OutcomeMappings.TryGetValue(outcomeId, out var setter))
            {
                setter(sportEvent, outcome.Odds);
            }
        }
    }

    private static OddsMarket? FindMarket(Dictionary<string, OddsMarket> markets, string[] keys)
    {
        foreach (var key in keys)
        {
            if (markets.TryGetValue(key, out var market))
                return market;
        }
        return null;
    }
}


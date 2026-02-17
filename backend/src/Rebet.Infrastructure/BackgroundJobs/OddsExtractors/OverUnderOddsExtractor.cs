using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;

namespace Rebet.Infrastructure.BackgroundJobs.OddsExtractors;

public class OverUnderOddsExtractor : IOddsExtractor
{
    private static readonly string[] MarketKeys = { "over_under", "total_goals" };
    private const string TargetHandicap = "2.5";

    public void ExtractOdds(SportEvent sportEvent, Dictionary<string, OddsMarket> markets)
    {
        var market = FindMarket(markets, MarketKeys);
        if (market == null)
            return;

        foreach (var outcome in market.Outcomes)
        {
            var handicap = outcome.Handicap?.Trim();
            var name = outcome.Name ?? string.Empty;
            var id = outcome.Id.ToLower();

            if (IsOverUnder25(handicap, name, id))
            {
                if (IsOver(name, id))
                    sportEvent.Over25Odds = outcome.Odds;
                else if (IsUnder(name, id))
                    sportEvent.Under25Odds = outcome.Odds;
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

    private static bool IsOverUnder25(string? handicap, string name, string id)
    {
        if (handicap == TargetHandicap)
            return true;

        return name.Contains("2.5", StringComparison.OrdinalIgnoreCase) ||
               id.Contains("2.5");
    }

    private static bool IsOver(string name, string id)
    {
        return name.Contains("Over", StringComparison.OrdinalIgnoreCase) ||
               id.Contains("over");
    }

    private static bool IsUnder(string name, string id)
    {
        return name.Contains("Under", StringComparison.OrdinalIgnoreCase) ||
               id.Contains("under");
    }
}


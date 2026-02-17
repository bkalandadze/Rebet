using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;

namespace Rebet.Infrastructure.BackgroundJobs.OddsExtractors;

public class OddsExtractorFactory
{
    private readonly List<IOddsExtractor> _extractors;

    public OddsExtractorFactory()
    {
        _extractors = new List<IOddsExtractor>
        {
            new MatchResultOddsExtractor(),
            new OverUnderOddsExtractor()
        };
    }

    public void ExtractAllOdds(SportEvent sportEvent, Dictionary<string, OddsMarket> markets)
    {
        foreach (var extractor in _extractors)
        {
            extractor.ExtractOdds(sportEvent, markets);
        }
    }
}


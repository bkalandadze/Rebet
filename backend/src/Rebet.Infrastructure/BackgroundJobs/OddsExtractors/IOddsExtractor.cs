using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;

namespace Rebet.Infrastructure.BackgroundJobs.OddsExtractors;

public interface IOddsExtractor
{
    void ExtractOdds(SportEvent sportEvent, Dictionary<string, OddsMarket> markets);
}


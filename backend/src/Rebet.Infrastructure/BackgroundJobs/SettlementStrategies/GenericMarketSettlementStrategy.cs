using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Rebet.Infrastructure.BackgroundJobs.SettlementStrategies;

public class GenericMarketSettlementStrategy : ISettlementStrategy
{
    private readonly ILogger _logger;

    public GenericMarketSettlementStrategy(ILogger logger)
    {
        _logger = logger;
    }

    public SettlementResult DetermineResult(Position position, EventResult eventResult, MarketResults? marketResults)
    {
        // Try to find result in market results JSON
        if (marketResults != null)
        {
            // Check if there's a matching result in the JSON
            // This is a fallback for custom markets
            _logger.LogInformation("Using generic market determination for position {PositionId}", position.Id);
        }

        // If we can't determine, void the position
        _logger.LogWarning("Cannot determine result for market {Market}, selection {Selection} for position {PositionId}",
            position.Market, position.Selection, position.Id);
        return new SettlementResult
        {
            Result = PositionResult.Void,
            Status = PositionStatus.Void
        };
    }
}


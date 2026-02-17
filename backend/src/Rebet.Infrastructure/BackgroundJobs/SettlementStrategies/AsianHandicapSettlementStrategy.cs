using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Rebet.Infrastructure.BackgroundJobs.SettlementStrategies;

public class AsianHandicapSettlementStrategy : ISettlementStrategy
{
    private readonly ILogger _logger;

    public AsianHandicapSettlementStrategy(ILogger logger)
    {
        _logger = logger;
    }

    public SettlementResult DetermineResult(Position position, EventResult eventResult, MarketResults? marketResults)
    {
        // Asian handicap requires more complex calculation
        // For now, return void if we can't determine it
        _logger.LogWarning("Asian handicap settlement not fully implemented for position {PositionId}", position.Id);
        return new SettlementResult
        {
            Result = PositionResult.Void,
            Status = PositionStatus.Void
        };
    }
}


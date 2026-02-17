using Rebet.Domain.Entities;

namespace Rebet.Infrastructure.BackgroundJobs.SettlementStrategies;

public interface ISettlementStrategy
{
    SettlementResult DetermineResult(Position position, EventResult eventResult, MarketResults? marketResults);
}


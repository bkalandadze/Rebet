using Rebet.Domain.Enums;

namespace Rebet.Infrastructure.BackgroundJobs.SettlementStrategies;

public class SettlementResult
{
    public PositionResult Result { get; set; }
    public PositionStatus Status { get; set; }
}


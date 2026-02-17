using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Rebet.Infrastructure.BackgroundJobs.SettlementStrategies;

public class MatchResultSettlementStrategy : ISettlementStrategy
{
    private readonly ILogger _logger;

    public MatchResultSettlementStrategy(ILogger logger)
    {
        _logger = logger;
    }

    public SettlementResult DetermineResult(Position position, EventResult eventResult, MarketResults? marketResults)
    {
        if (string.IsNullOrEmpty(eventResult.Winner))
        {
            _logger.LogWarning("Winner not set in event result for position {PositionId}", position.Id);
            return CreateVoidResult();
        }

        var winner = eventResult.Winner.ToLowerInvariant();
        var normalizedSelection = position.Selection.ToLowerInvariant();

        var isWin = IsMatchResultWin(normalizedSelection, winner);

        return CreateResult(isWin);
    }

    private static bool IsMatchResultWin(string selection, string winner)
    {
        var matchResultMap = new Dictionary<string, string>
        {
            { "home", "home" },
            { "away", "away" },
            { "draw", "draw" },
            { "1", "home" },
            { "2", "away" },
            { "x", "draw" }
        };

        return matchResultMap.TryGetValue(selection, out var mappedWinner) && mappedWinner == winner;
    }

    private static SettlementResult CreateResult(bool isWin) => new()
    {
        Result = isWin ? PositionResult.Won : PositionResult.Lost,
        Status = isWin ? PositionStatus.Won : PositionStatus.Lost
    };

    private static SettlementResult CreateVoidResult() => new()
    {
        Result = PositionResult.Void,
        Status = PositionStatus.Void
    };
}


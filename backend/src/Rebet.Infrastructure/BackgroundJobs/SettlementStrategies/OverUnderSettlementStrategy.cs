using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Rebet.Infrastructure.BackgroundJobs.SettlementStrategies;

public class OverUnderSettlementStrategy : ISettlementStrategy
{
    private readonly ILogger _logger;
    private readonly ScoreParser _scoreParser;

    public OverUnderSettlementStrategy(ILogger logger, ScoreParser scoreParser)
    {
        _logger = logger;
        _scoreParser = scoreParser;
    }

    public SettlementResult DetermineResult(Position position, EventResult eventResult, MarketResults? marketResults)
    {
        var totalGoals = GetTotalGoals(eventResult, marketResults);
        if (totalGoals == null)
        {
            _logger.LogWarning("Cannot determine total goals for position {PositionId}", position.Id);
            return CreateVoidResult();
        }

        var line = ExtractOverUnderLine(position.Selection);
        if (line == null)
        {
            _logger.LogWarning("Cannot extract line from selection {Selection} for position {PositionId}",
                position.Selection, position.Id);
            return CreateVoidResult();
        }

        var selectionType = DetermineSelectionType(position.Selection);
        if (selectionType == SelectionType.Unknown)
        {
            _logger.LogWarning("Invalid over/under selection {Selection} for position {PositionId}",
                position.Selection, position.Id);
            return CreateVoidResult();
        }

        var isWin = selectionType == SelectionType.Over
            ? totalGoals.Value > line.Value
            : totalGoals.Value < line.Value;

        return CreateResult(isWin);
    }

    private int? GetTotalGoals(EventResult eventResult, MarketResults? marketResults)
    {
        if (marketResults?.TotalGoals != null)
            return marketResults.TotalGoals;

        if (!string.IsNullOrEmpty(eventResult.FinalScore))
            return _scoreParser.ParseTotalGoals(eventResult.FinalScore);

        return null;
    }

    private static decimal? ExtractOverUnderLine(string selection)
    {
        var parts = selection.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (decimal.TryParse(part, out var line))
                return line;
        }
        return null;
    }

    private static SelectionType DetermineSelectionType(string selection)
    {
        var normalized = selection.ToLowerInvariant();
        if (normalized.Contains("over"))
            return SelectionType.Over;
        if (normalized.Contains("under"))
            return SelectionType.Under;
        return SelectionType.Unknown;
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

    private enum SelectionType
    {
        Unknown,
        Over,
        Under
    }
}


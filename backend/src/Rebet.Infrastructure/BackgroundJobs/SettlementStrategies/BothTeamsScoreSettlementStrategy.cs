using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Rebet.Infrastructure.BackgroundJobs.SettlementStrategies;

public class BothTeamsScoreSettlementStrategy : ISettlementStrategy
{
    private readonly ILogger _logger;
    private readonly ScoreParser _scoreParser;

    public BothTeamsScoreSettlementStrategy(ILogger logger, ScoreParser scoreParser)
    {
        _logger = logger;
        _scoreParser = scoreParser;
    }

    public SettlementResult DetermineResult(Position position, EventResult eventResult, MarketResults? marketResults)
    {
        var bothTeamsScored = GetBothTeamsScored(eventResult, marketResults);
        if (bothTeamsScored == null)
        {
            _logger.LogWarning("Cannot determine both teams score for position {PositionId}", position.Id);
            return CreateVoidResult();
        }

        var selectionType = DetermineSelectionType(position.Selection);
        if (selectionType == SelectionType.Unknown)
        {
            _logger.LogWarning("Invalid both teams score selection {Selection} for position {PositionId}",
                position.Selection, position.Id);
            return CreateVoidResult();
        }

        var isWin = selectionType == SelectionType.Yes
            ? bothTeamsScored.Value
            : !bothTeamsScored.Value;

        return CreateResult(isWin);
    }

    private bool? GetBothTeamsScored(EventResult eventResult, MarketResults? marketResults)
    {
        if (marketResults?.BothTeamsScore != null)
            return marketResults.BothTeamsScore;

        if (!string.IsNullOrEmpty(eventResult.FinalScore))
        {
            var scores = _scoreParser.ParseScore(eventResult.FinalScore);
            if (scores.HasValue)
                return scores.Value.HomeScore > 0 && scores.Value.AwayScore > 0;
        }

        return null;
    }

    private static SelectionType DetermineSelectionType(string selection)
    {
        var normalized = selection.ToLowerInvariant();
        var yesValues = new[] { "yes", "true", "1" };
        var noValues = new[] { "no", "false", "0" };

        if (yesValues.Contains(normalized))
            return SelectionType.Yes;
        if (noValues.Contains(normalized))
            return SelectionType.No;
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
        Yes,
        No
    }
}


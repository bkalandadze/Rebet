using System.Text.Json;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;

namespace Rebet.Application.Helpers;

public static class SettlementHelper
{
    /// <summary>
    /// Determines the result for a Match Result position.
    /// </summary>
    /// <param name="position">The position with Market="Match Result"</param>
    /// <param name="result">The event result</param>
    /// <returns>PositionResult (Won/Lost/Void)</returns>
    public static PositionResult DetermineMatchResult(Position position, EventResult result)
    {
        if (result == null)
            return PositionResult.Void;

        // Check if event was cancelled or abandoned
        if (IsEventVoid(result))
            return PositionResult.Void;

        // Normalize selection and winner for comparison
        var selection = NormalizeMatchResultSelection(position.Selection);
        var winner = NormalizeMatchResultSelection(result.Winner);

        if (string.IsNullOrEmpty(winner))
            return PositionResult.Void;

        return selection.Equals(winner, StringComparison.OrdinalIgnoreCase)
            ? PositionResult.Won
            : PositionResult.Lost;
    }

    /// <summary>
    /// Determines the result for an Over/Under position.
    /// </summary>
    /// <param name="position">The position with Market="Over/Under"</param>
    /// <param name="result">The event result</param>
    /// <returns>PositionResult (Won/Lost/Void)</returns>
    public static PositionResult DetermineOverUnder(Position position, EventResult result)
    {
        if (result == null)
            return PositionResult.Void;

        // Check if event was cancelled or abandoned
        if (IsEventVoid(result))
            return PositionResult.Void;

        // Parse the line from selection (e.g., "Over 2.5" -> 2.5, "Under 1.5" -> 1.5)
        var (selectionType, line) = ParseOverUnderSelection(position.Selection);
        if (selectionType == null || !line.HasValue)
            return PositionResult.Void;

        // Get total goals from market results
        var totalGoals = GetTotalGoals(result);
        if (!totalGoals.HasValue)
            return PositionResult.Void;

        // Determine result
        // For whole number lines, exact match is a push (void)
        // For half lines (e.g., 2.5), exact match is impossible (totalGoals is integer)
        bool isWholeNumber = line.Value % 1 == 0;
        if (isWholeNumber && totalGoals.Value == line.Value)
            return PositionResult.Void;

        bool won = selectionType.ToLowerInvariant() switch
        {
            "over" => totalGoals.Value > line.Value,
            "under" => totalGoals.Value < line.Value,
            _ => false
        };

        return won ? PositionResult.Won : PositionResult.Lost;
    }

    /// <summary>
    /// Determines the result for a Both Teams Score position.
    /// </summary>
    /// <param name="position">The position with Market="Both Teams Score"</param>
    /// <param name="result">The event result</param>
    /// <returns>PositionResult (Won/Lost/Void)</returns>
    public static PositionResult DetermineBothTeamsScore(Position position, EventResult result)
    {
        if (result == null)
            return PositionResult.Void;

        // Check if event was cancelled or abandoned
        if (IsEventVoid(result))
            return PositionResult.Void;

        // Get both teams score result
        var bothTeamsScored = GetBothTeamsScore(result);
        if (!bothTeamsScored.HasValue)
            return PositionResult.Void;

        // Normalize selection
        var selection = position.Selection.Trim();
        var isYes = selection.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
                    selection.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                    selection.Equals("1", StringComparison.OrdinalIgnoreCase);
        var isNo = selection.Equals("No", StringComparison.OrdinalIgnoreCase) ||
                   selection.Equals("False", StringComparison.OrdinalIgnoreCase) ||
                   selection.Equals("0", StringComparison.OrdinalIgnoreCase);

        if (!isYes && !isNo)
            return PositionResult.Void;

        var predictedYes = isYes;
        return predictedYes == bothTeamsScored.Value
            ? PositionResult.Won
            : PositionResult.Lost;
    }

    /// <summary>
    /// Determines the result for an Asian Handicap position.
    /// </summary>
    /// <param name="position">The position with Market="Asian Handicap"</param>
    /// <param name="result">The event result</param>
    /// <returns>PositionResult (Won/Lost/Void)</returns>
    public static PositionResult DetermineAsianHandicap(Position position, EventResult result)
    {
        if (result == null)
            return PositionResult.Void;

        // Check if event was cancelled or abandoned
        if (IsEventVoid(result))
            return PositionResult.Void;

        // Parse handicap from selection (e.g., "Home -1.5", "Away +0.5")
        var (team, handicap) = ParseAsianHandicapSelection(position.Selection);
        if (team == null || !handicap.HasValue)
            return PositionResult.Void;

        // Get scores
        var (homeScore, awayScore) = GetScores(result);
        if (!homeScore.HasValue || !awayScore.HasValue)
            return PositionResult.Void;

        // Apply handicap
        decimal homeScoreWithHandicap = homeScore.Value;
        decimal awayScoreWithHandicap = awayScore.Value;

        if (team.Equals("Home", StringComparison.OrdinalIgnoreCase))
        {
            homeScoreWithHandicap += handicap.Value;
        }
        else if (team.Equals("Away", StringComparison.OrdinalIgnoreCase))
        {
            awayScoreWithHandicap += handicap.Value;
        }
        else
        {
            return PositionResult.Void;
        }

        // Determine winner with handicap applied
        if (homeScoreWithHandicap > awayScoreWithHandicap)
        {
            // Home wins with handicap
            return team.Equals("Home", StringComparison.OrdinalIgnoreCase)
                ? PositionResult.Won
                : PositionResult.Lost;
        }
        else if (awayScoreWithHandicap > homeScoreWithHandicap)
        {
            // Away wins with handicap
            return team.Equals("Away", StringComparison.OrdinalIgnoreCase)
                ? PositionResult.Won
                : PositionResult.Lost;
        }
        else
        {
            // Draw with handicap - void the bet
            return PositionResult.Void;
        }
    }

    #region Private Helper Methods

    private static bool IsEventVoid(EventResult result)
    {
        // Check if event was cancelled or abandoned based on market results
        if (string.IsNullOrEmpty(result.MarketResultsJson))
            return false;

        try
        {
            var json = JsonDocument.Parse(result.MarketResultsJson);
            if (json.RootElement.TryGetProperty("cancelled", out var cancelled) &&
                cancelled.GetBoolean())
                return true;

            if (json.RootElement.TryGetProperty("abandoned", out var abandoned) &&
                abandoned.GetBoolean())
                return true;
        }
        catch
        {
            // If JSON parsing fails, assume not void
        }

        return false;
    }

    private static string NormalizeMatchResultSelection(string? selection)
    {
        if (string.IsNullOrWhiteSpace(selection))
            return string.Empty;

        return selection.Trim().ToLowerInvariant() switch
        {
            "home" or "home win" or "1" => "home",
            "away" or "away win" or "2" => "away",
            "draw" or "x" => "draw",
            _ => selection.Trim().ToLowerInvariant()
        };
    }

    private static (string? selectionType, decimal? line) ParseOverUnderSelection(string selection)
    {
        if (string.IsNullOrWhiteSpace(selection))
            return (null, null);

        var parts = selection.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return (null, null);

        var type = parts[0];
        if (!decimal.TryParse(parts[1], out var line))
            return (null, null);

        return (type, line);
    }

    private static int? GetTotalGoals(EventResult result)
    {
        // Try to get from MarketResultsJson first
        if (!string.IsNullOrEmpty(result.MarketResultsJson))
        {
            try
            {
                var json = JsonDocument.Parse(result.MarketResultsJson);
                if (json.RootElement.TryGetProperty("totalGoals", out var totalGoals))
                {
                    if (totalGoals.ValueKind == JsonValueKind.Number)
                        return totalGoals.GetInt32();
                }

                // Fallback: calculate from scores in JSON
                if (json.RootElement.TryGetProperty("homeScore", out var homeScoreElement) &&
                    json.RootElement.TryGetProperty("awayScore", out var awayScoreElement))
                {
                    if (homeScoreElement.ValueKind == JsonValueKind.Number &&
                        awayScoreElement.ValueKind == JsonValueKind.Number)
                    {
                        return homeScoreElement.GetInt32() + awayScoreElement.GetInt32();
                    }
                }
            }
            catch
            {
                // Fall through to parsing from FinalScore
            }
        }

        // Fallback: calculate from FinalScore
        var (homeScore, awayScore) = GetScores(result);
        if (homeScore.HasValue && awayScore.HasValue)
            return homeScore.Value + awayScore.Value;

        return null;
    }

    private static bool? GetBothTeamsScore(EventResult result)
    {
        if (string.IsNullOrEmpty(result.MarketResultsJson))
        {
            // Fallback: calculate from scores
            var (homeScore, awayScore) = GetScores(result);
            if (homeScore.HasValue && awayScore.HasValue)
                return homeScore.Value > 0 && awayScore.Value > 0;
            return null;
        }

        try
        {
            var json = JsonDocument.Parse(result.MarketResultsJson);
            if (json.RootElement.TryGetProperty("bothTeamsScore", out var bothTeamsScore))
            {
                if (bothTeamsScore.ValueKind == JsonValueKind.True ||
                    bothTeamsScore.ValueKind == JsonValueKind.False)
                    return bothTeamsScore.GetBoolean();
            }

            // Fallback: calculate from scores
            var (homeScore, awayScore) = GetScores(result);
            if (homeScore.HasValue && awayScore.HasValue)
                return homeScore.Value > 0 && awayScore.Value > 0;
        }
        catch
        {
            // If parsing fails, try to calculate from scores
            var (homeScore, awayScore) = GetScores(result);
            if (homeScore.HasValue && awayScore.HasValue)
                return homeScore.Value > 0 && awayScore.Value > 0;
        }

        return null;
    }

    private static (int? homeScore, int? awayScore) GetScores(EventResult result)
    {
        // Try to parse from MarketResultsJson first
        if (!string.IsNullOrEmpty(result.MarketResultsJson))
        {
            try
            {
                var json = JsonDocument.Parse(result.MarketResultsJson);
                int? homeScore = null;
                int? awayScore = null;

                if (json.RootElement.TryGetProperty("homeScore", out var homeScoreElement))
                {
                    if (homeScoreElement.ValueKind == JsonValueKind.Number)
                        homeScore = homeScoreElement.GetInt32();
                }

                if (json.RootElement.TryGetProperty("awayScore", out var awayScoreElement))
                {
                    if (awayScoreElement.ValueKind == JsonValueKind.Number)
                        awayScore = awayScoreElement.GetInt32();
                }

                if (homeScore.HasValue && awayScore.HasValue)
                    return (homeScore, awayScore);
            }
            catch
            {
                // Fall through to parsing FinalScore
            }
        }

        // Fallback: parse from FinalScore (e.g., "2-1", "3-0")
        if (!string.IsNullOrEmpty(result.FinalScore))
        {
            var parts = result.FinalScore.Split('-', ':');
            if (parts.Length == 2 &&
                int.TryParse(parts[0].Trim(), out var home) &&
                int.TryParse(parts[1].Trim(), out var away))
            {
                return (home, away);
            }
        }

        return (null, null);
    }

    private static (string? team, decimal? handicap) ParseAsianHandicapSelection(string selection)
    {
        if (string.IsNullOrWhiteSpace(selection))
            return (null, null);

        var trimmed = selection.Trim();
        
        // Try patterns like "Home -1.5", "Away +0.5", "Home +1", "Away -0.5"
        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return (null, null);

        var team = parts[0];
        if (!team.Equals("Home", StringComparison.OrdinalIgnoreCase) &&
            !team.Equals("Away", StringComparison.OrdinalIgnoreCase))
            return (null, null);

        // Parse handicap (can be +1.5, -0.5, etc.)
        var handicapStr = parts[1];
        if (!decimal.TryParse(handicapStr, out var handicap))
            return (null, null);

        return (team, handicap);
    }

    #endregion
}


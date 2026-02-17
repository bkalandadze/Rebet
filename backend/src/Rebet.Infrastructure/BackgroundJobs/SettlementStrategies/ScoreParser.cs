namespace Rebet.Infrastructure.BackgroundJobs.SettlementStrategies;

public class ScoreParser
{
    public (int HomeScore, int AwayScore)? ParseScore(string score)
    {
        if (string.IsNullOrEmpty(score))
            return null;

        var separators = new[] { "-", ":", " " };
        foreach (var separator in separators)
        {
            var parts = score.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 &&
                int.TryParse(parts[0].Trim(), out var homeScore) &&
                int.TryParse(parts[1].Trim(), out var awayScore))
            {
                return (homeScore, awayScore);
            }
        }

        return null;
    }

    public int? ParseTotalGoals(string score)
    {
        if (string.IsNullOrEmpty(score))
            return null;

        var scores = ParseScore(score);
        return scores.HasValue ? scores.Value.HomeScore + scores.Value.AwayScore : null;
    }
}


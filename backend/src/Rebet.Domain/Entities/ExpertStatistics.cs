namespace Rebet.Domain.Entities;

public class ExpertStatistics
{
    public Guid ExpertId { get; set; }
    
    // Overall Stats
    public int TotalPositions { get; set; } = 0;
    public int TotalTickets { get; set; } = 0;
    public int WonPositions { get; set; } = 0;
    public int LostPositions { get; set; } = 0;
    public int VoidPositions { get; set; } = 0;
    public int PendingPositions { get; set; } = 0;
    
    // Performance Metrics
    public decimal WinRate { get; set; } = 0.00m;
    public decimal ROI { get; set; } = 0.00m;
    public decimal AverageOdds { get; set; } = 0.00m;
    public int CurrentStreak { get; set; } = 0;
    public int LongestWinStreak { get; set; } = 0;
    
    // Time-based Stats
    public decimal Last7DaysWinRate { get; set; } = 0.00m;
    public decimal Last30DaysWinRate { get; set; } = 0.00m;
    public decimal Last90DaysWinRate { get; set; } = 0.00m;
    
    // Financial
    public decimal TotalProfit { get; set; } = 0.00m;
    public decimal TotalCommissionEarned { get; set; } = 0.00m;
    
    // Engagement
    public int TotalSubscribers { get; set; } = 0;
    public int ActiveSubscribers { get; set; } = 0;
    
    public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Expert Expert { get; set; } = null!;
    
    // Methods
    public void RecalculateWinRate()
    {
        var totalSettled = WonPositions + LostPositions;
        WinRate = totalSettled > 0 ? (decimal)WonPositions / totalSettled * 100 : 0;
    }
    
    public void UpdateStreak(bool isWin)
    {
        if (isWin)
        {
            CurrentStreak = CurrentStreak >= 0 ? CurrentStreak + 1 : 1;
            LongestWinStreak = Math.Max(LongestWinStreak, CurrentStreak);
        }
        else
        {
            CurrentStreak = CurrentStreak <= 0 ? CurrentStreak - 1 : -1;
        }
    }
}


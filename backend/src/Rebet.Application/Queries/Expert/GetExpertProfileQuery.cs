using Rebet.Application.DTOs;
using MediatR;

namespace Rebet.Application.Queries.Expert;

public class GetExpertProfileQuery : IRequest<ExpertProfileDto>
{
    public Guid ExpertId { get; set; }
    public Guid? UserId { get; set; } // Optional - current user ID for vote status and subscription status
}

public class ExpertProfileDto
{
    public Guid Id { get; set; }
    public UserBasicDto User { get; set; } = null!;
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
    public string Tier { get; set; } = null!;
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public ExpertProfileStatisticsDto Statistics { get; set; } = null!;
    public int UpvoteCount { get; set; }
    public int DownvoteCount { get; set; }
    public int SubscriberCount { get; set; }
    public List<PositionListDto>? RecentPositions { get; set; }
    public string? UserVote { get; set; } // "upvote", "downvote", or null
    public bool? IsSubscribed { get; set; } // null if not authenticated
}

public class ExpertProfileStatisticsDto
{
    public ExpertStatisticsOverallDto Overall { get; set; } = null!;
    public ExpertStatisticsTimeframesDto Timeframes { get; set; } = null!;
    public ExpertStatisticsByOddsRangeDto ByOddsRange { get; set; } = null!;
    public int CurrentStreak { get; set; }
    public int LongestWinStreak { get; set; }
}

public class ExpertStatisticsOverallDto
{
    public int TotalPositions { get; set; }
    public int WonPositions { get; set; }
    public int LostPositions { get; set; }
    public decimal WinRate { get; set; }
    public decimal ROI { get; set; }
    public decimal AverageOdds { get; set; }
}

public class ExpertStatisticsTimeframesDto
{
    public ExpertStatisticsTimeframeDto? Last7Days { get; set; }
    public ExpertStatisticsTimeframeDto? Last30Days { get; set; }
    public ExpertStatisticsTimeframeDto? Last90Days { get; set; }
}

public class ExpertStatisticsTimeframeDto
{
    public decimal WinRate { get; set; }
    public int TotalPositions { get; set; }
}

public class ExpertStatisticsByOddsRangeDto
{
    public ExpertStatisticsOddsRangeDto? Range1_01_2_00 { get; set; }
    public ExpertStatisticsOddsRangeDto? Range2_01_3_00 { get; set; }
    public ExpertStatisticsOddsRangeDto? Range3_01_Plus { get; set; }
}

public class ExpertStatisticsOddsRangeDto
{
    public decimal WinRate { get; set; }
    public int Count { get; set; }
}


using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using MediatR;

namespace Rebet.Application.Queries.Expert;

public class GetExpertProfileQueryHandler : IRequestHandler<GetExpertProfileQuery, ExpertProfileDto>
{
    private readonly IExpertRepository _expertRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IVoteRepository _voteRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetExpertProfileQueryHandler(
        IExpertRepository expertRepository,
        IPositionRepository positionRepository,
        IUserRepository userRepository,
        IVoteRepository voteRepository,
        ISubscriptionRepository subscriptionRepository)
    {
        _expertRepository = expertRepository;
        _positionRepository = positionRepository;
        _userRepository = userRepository;
        _voteRepository = voteRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<ExpertProfileDto> Handle(GetExpertProfileQuery request, CancellationToken cancellationToken)
    {
        var expert = await _expertRepository.GetByIdAsync(request.ExpertId, cancellationToken);
        if (expert == null || expert.IsDeleted)
        {
            throw new KeyNotFoundException($"Expert with ID {request.ExpertId} not found");
        }

        // Load user and profile
        var user = await _userRepository.GetByIdAsync(expert.UserId, cancellationToken);
        
        if (user == null)
        {
            throw new KeyNotFoundException($"User for expert {request.ExpertId} not found");
        }

        // Get user vote if authenticated
        string? userVote = null;
        bool? isSubscribed = null;
        
        if (request.UserId.HasValue)
        {
            var vote = await _voteRepository.GetVoteAsync(
                request.UserId.Value,
                VoteableType.Expert,
                request.ExpertId,
                cancellationToken);
            
            userVote = vote?.Type == VoteType.Upvote ? "upvote" 
                      : vote?.Type == VoteType.Downvote ? "downvote" 
                      : null;

            // Get subscription status
            var subscription = await _subscriptionRepository.GetByUserAndExpertAsync(
                request.UserId.Value,
                request.ExpertId,
                cancellationToken);
            
            isSubscribed = subscription != null && subscription.Status == SubscriptionStatus.Active;
        }

        // Get statistics
        var stats = expert.Statistics;
        if (stats == null)
        {
            // Create default statistics if not exists
            stats = new ExpertStatistics { ExpertId = expert.Id };
        }

        // Get recent positions (last 10)
        var allPositions = await _positionRepository.GetByCreatorIdAsync(
            expert.UserId,
            cancellationToken);
        
        var recentPositions = allPositions
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .ToList();

        // Calculate position counts for timeframes
        var now = DateTime.UtcNow;
        var last7Days = allPositions.Where(p => !p.IsDeleted && p.CreatedAt >= now.AddDays(-7)).ToList();
        var last30Days = allPositions.Where(p => !p.IsDeleted && p.CreatedAt >= now.AddDays(-30)).ToList();
        var last90Days = allPositions.Where(p => !p.IsDeleted && p.CreatedAt >= now.AddDays(-90)).ToList();

        var positionDtos = recentPositions.Select(p => new PositionListDto
        {
            Id = p.Id,
            CreatorId = p.CreatorId,
            CreatorName = expert.DisplayName,
            CreatorAvatar = user.Profile?.Avatar,
            IsExpert = true,
            SportEvent = new SportEventListDto
            {
                Id = p.SportEventId,
                HomeTeam = p.SportEvent?.HomeTeam ?? "",
                AwayTeam = p.SportEvent?.AwayTeam ?? "",
                League = p.SportEvent?.League ?? "",
                StartTime = p.SportEvent?.StartTimeUtc ?? DateTime.UtcNow
            },
            Market = p.Market,
            Selection = p.Selection,
            Odds = p.Odds,
            Status = p.Status.ToString(),
            UpvoteCount = p.UpvoteCount,
            DownvoteCount = p.DownvoteCount,
            VoterCount = p.VoterCount,
            PredictionPercentage = p.PredictionPercentage,
            ViewCount = p.ViewCount,
            CreatedAt = p.CreatedAt,
            UserVote = null
        }).ToList();

        // Build statistics DTO
        var statisticsDto = new ExpertProfileStatisticsDto
        {
            Overall = new ExpertStatisticsOverallDto
            {
                TotalPositions = stats.TotalPositions,
                WonPositions = stats.WonPositions,
                LostPositions = stats.LostPositions,
                WinRate = stats.WinRate,
                ROI = stats.ROI,
                AverageOdds = stats.AverageOdds
            },
            Timeframes = new ExpertStatisticsTimeframesDto
            {
                Last7Days = stats.Last7DaysWinRate > 0 
                    ? new ExpertStatisticsTimeframeDto 
                    { 
                        WinRate = stats.Last7DaysWinRate, 
                        TotalPositions = last7Days.Count
                    } 
                    : null,
                Last30Days = stats.Last30DaysWinRate > 0 
                    ? new ExpertStatisticsTimeframeDto 
                    { 
                        WinRate = stats.Last30DaysWinRate, 
                        TotalPositions = last30Days.Count
                    } 
                    : null,
                Last90Days = stats.Last90DaysWinRate > 0 
                    ? new ExpertStatisticsTimeframeDto 
                    { 
                        WinRate = stats.Last90DaysWinRate, 
                        TotalPositions = last90Days.Count
                    } 
                    : null
            },
            ByOddsRange = new ExpertStatisticsByOddsRangeDto
            {
                // TODO: These would need to be calculated from positions
                // For now, leaving as null - would need additional queries
            },
            CurrentStreak = stats.CurrentStreak,
            LongestWinStreak = stats.LongestWinStreak
        };

        return new ExpertProfileDto
        {
            Id = expert.Id,
            User = new UserBasicDto
            {
                Id = user.Id,
                DisplayName = user.Profile?.DisplayName ?? expert.DisplayName,
                Avatar = user.Profile?.Avatar
            },
            Bio = expert.Bio,
            Specialization = expert.Specialization,
            Tier = expert.Tier.ToString(),
            IsVerified = expert.IsVerified,
            VerifiedAt = expert.VerifiedAt,
            Statistics = statisticsDto,
            UpvoteCount = expert.UpvoteCount,
            DownvoteCount = expert.DownvoteCount,
            SubscriberCount = stats.TotalSubscribers,
            RecentPositions = positionDtos,
            UserVote = userVote,
            IsSubscribed = isSubscribed
        };
    }
}


using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using Rebet.Domain.Enums;
using MediatR;

namespace Rebet.Application.Queries.Position;

public class GetPositionDetailQueryHandler : IRequestHandler<GetPositionDetailQuery, PositionDetailDto>
{
    private readonly IPositionRepository _positionRepository;
    private readonly IVoteRepository _voteRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetPositionDetailQueryHandler(
        IPositionRepository positionRepository,
        IVoteRepository voteRepository,
        ISubscriptionRepository subscriptionRepository)
    {
        _positionRepository = positionRepository;
        _voteRepository = voteRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<PositionDetailDto> Handle(GetPositionDetailQuery request, CancellationToken cancellationToken)
    {
        // Get position with all required navigation properties
        var position = await _positionRepository.GetByIdAsync(request.PositionId, cancellationToken);

        if (position == null || position.IsDeleted)
        {
            throw new KeyNotFoundException($"Position with ID {request.PositionId} not found");
        }

        // Increment view count
        position.IncrementViewCount();
        await _positionRepository.SaveChangesAsync(cancellationToken);

        // Get user's vote if authenticated
        string? userVote = null;
        if (request.UserId.HasValue)
        {
            var vote = await _voteRepository.GetVoteAsync(
                request.UserId.Value,
                VoteableType.Position,
                request.PositionId,
                cancellationToken);
            
            if (vote != null)
            {
                userVote = vote.Type == VoteType.Upvote ? "upvote" : "downvote";
            }
        }

        // Get expert info and subscription status if creator is expert
        decimal? winRate = null;
        int? subscriberCount = null;
        bool isSubscribed = false;

        if (position.CreatorType == UserRole.Expert && position.Creator.Expert != null)
        {
            var expert = position.Creator.Expert;
            winRate = expert.Statistics?.WinRate;
            subscriberCount = expert.Statistics?.ActiveSubscribers ?? 0;

            // Check if current user is subscribed
            if (request.UserId.HasValue)
            {
                var subscription = await _subscriptionRepository.GetByUserAndExpertAsync(
                    request.UserId.Value,
                    expert.Id,
                    cancellationToken);
                
                isSubscribed = subscription != null 
                    && subscription.Status == SubscriptionStatus.Active
                    && !subscription.IsDeleted;
            }
        }

        // Apply blurring logic: hide selection/analysis if expert win rate >= 80% and user not subscribed
        bool shouldBlur = position.CreatorType == UserRole.Expert
                          && winRate.HasValue
                          && winRate.Value >= 80.0m
                          && !isSubscribed;

        // Map to DTO
        var dto = new PositionDetailDto
        {
            Id = position.Id,
            Creator = new CreatorDetailDto
            {
                Id = position.CreatorId,
                DisplayName = position.Creator.Profile?.DisplayName ??
                             (!string.IsNullOrWhiteSpace(position.Creator.FirstName) || !string.IsNullOrWhiteSpace(position.Creator.LastName)
                                 ? $"{position.Creator.FirstName} {position.Creator.LastName}".Trim()
                                 : position.Creator.Email),
                Avatar = position.Creator.Profile?.Avatar,
                IsExpert = position.CreatorType == UserRole.Expert,
                WinRate = winRate,
                SubscriberCount = subscriberCount
            },
            SportEvent = new SportEventDetailDto
            {
                Id = position.SportEvent.Id,
                HomeTeam = position.SportEvent.HomeTeam,
                AwayTeam = position.SportEvent.AwayTeam,
                League = position.SportEvent.League,
                Sport = position.SportEvent.Sport,
                StartTime = position.SportEvent.StartTimeUtc,
                Status = position.SportEvent.Status.ToString(),
                HomeWinOdds = position.SportEvent.HomeWinOdds,
                DrawOdds = position.SportEvent.DrawOdds,
                AwayWinOdds = position.SportEvent.AwayWinOdds
            },
            Market = position.Market,
            Selection = shouldBlur ? "***" : position.Selection,
            Odds = position.Odds,
            Analysis = shouldBlur ? null : position.Analysis,
            Status = position.Status.ToString(),
            Result = position.Result?.ToString(),
            UpvoteCount = position.UpvoteCount,
            DownvoteCount = position.DownvoteCount,
            VoterCount = position.VoterCount,
            PredictionPercentage = position.PredictionPercentage,
            ViewCount = position.ViewCount,
            CreatedAt = position.CreatedAt,
            SettledAt = position.SettledAt,
            UserVote = userVote
        };

        return dto;
    }
}


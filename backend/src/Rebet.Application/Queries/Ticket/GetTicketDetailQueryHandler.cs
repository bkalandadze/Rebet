using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using MediatR;

namespace Rebet.Application.Queries.Ticket;

public class GetTicketDetailQueryHandler : IRequestHandler<GetTicketDetailQuery, TicketDetailDto>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IVoteRepository _voteRepository;
    private readonly ITicketFollowRepository _ticketFollowRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetTicketDetailQueryHandler(
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IVoteRepository voteRepository,
        ITicketFollowRepository ticketFollowRepository,
        ICommentRepository commentRepository,
        ISubscriptionRepository subscriptionRepository)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _voteRepository = voteRepository;
        _ticketFollowRepository = ticketFollowRepository;
        _commentRepository = commentRepository;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<TicketDetailDto> Handle(GetTicketDetailQuery request, CancellationToken cancellationToken)
    {
        // Get ticket with all required navigation properties
        var ticket = await _ticketRepository.GetDetailByIdAsync(request.TicketId, cancellationToken);

        if (ticket == null || ticket.IsDeleted)
        {
            throw new KeyNotFoundException($"Ticket with ID {request.TicketId} not found");
        }

        // Increment view count
        ticket.ViewCount++;
        await _ticketRepository.SaveChangesAsync(cancellationToken);

        // Get expert user with profile
        var expert = await _userRepository.GetByIdAsync(ticket.ExpertId, cancellationToken);

        if (expert == null)
        {
            throw new InvalidOperationException($"Expert user with ID {ticket.ExpertId} not found");
        }

        // Get expert statistics if Expert entity exists
        decimal? winRate = null;
        ExpertStatisticsDto? expertStats = null;
        int subscriberCount = 0;
        bool isSubscribed = false;
        string? tier = null;
        bool isVerified = false;
        DateTime? verifiedAt = null;
        string? bio = null;
        string? specialization = null;

        if (expert.Expert != null)
        {
            var expertEntity = expert.Expert;
            winRate = expertEntity.Statistics?.WinRate;
            subscriberCount = expertEntity.Statistics?.ActiveSubscribers ?? 0;
            tier = expertEntity.Tier.ToString();
            isVerified = expertEntity.IsVerified;
            verifiedAt = expertEntity.VerifiedAt;
            bio = expertEntity.Bio;
            specialization = expertEntity.Specialization;

            // Map expert statistics
            if (expertEntity.Statistics != null)
            {
                expertStats = new ExpertStatisticsDto
                {
                    TotalPositions = expertEntity.Statistics.TotalPositions,
                    TotalTickets = expertEntity.Statistics.TotalTickets,
                    WinRate = expertEntity.Statistics.WinRate,
                    ROI = expertEntity.Statistics.ROI,
                    AverageOdds = expertEntity.Statistics.AverageOdds,
                    CurrentStreak = expertEntity.Statistics.CurrentStreak,
                    LongestWinStreak = expertEntity.Statistics.LongestWinStreak,
                    Last7DaysWinRate = expertEntity.Statistics.Last7DaysWinRate,
                    Last30DaysWinRate = expertEntity.Statistics.Last30DaysWinRate,
                    Last90DaysWinRate = expertEntity.Statistics.Last90DaysWinRate
                };
            }

            // Check if current user is subscribed
            if (request.UserId.HasValue)
            {
                var subscription = await _subscriptionRepository.GetByUserAndExpertAsync(
                    request.UserId.Value,
                    expertEntity.Id,
                    cancellationToken);
                
                isSubscribed = subscription != null 
                    && subscription.Status == SubscriptionStatus.Active
                    && !subscription.IsDeleted;
            }
        }

        // Get user's vote if authenticated
        string? userVote = null;
        if (request.UserId.HasValue)
        {
            var vote = await _voteRepository.GetVoteAsync(
                request.UserId.Value,
                VoteableType.Ticket,
                ticket.Id,
                cancellationToken);
            
            if (vote != null)
            {
                userVote = vote.Type == VoteType.Upvote ? "upvote" : "downvote";
            }
        }

        // Get follow status if authenticated
        bool? isFollowing = null;
        if (request.UserId.HasValue)
        {
            var follow = await _ticketFollowRepository.GetByUserAndTicketAsync(
                request.UserId.Value,
                ticket.Id,
                cancellationToken);
            
            isFollowing = follow != null && !follow.IsDeleted;
        }

        // Get comments (latest 20)
        var ticketComments = await _commentRepository.GetCommentsByTicketAsync(
            ticket.Id,
            20,
            cancellationToken);

        // Group comments by parent
        var topLevelComments = ticketComments.Where(c => c.ParentCommentId == null).ToList();
        var replies = ticketComments.Where(c => c.ParentCommentId != null)
            .GroupBy(c => c.ParentCommentId)
            .ToDictionary(g => g.Key!.Value, g => g.ToList());

        var comments = topLevelComments.Select(c => new CommentDto
        {
            Id = c.Id,
            User = new UserBasicDto
            {
                Id = c.User.Id,
                DisplayName = c.User.Profile?.DisplayName ?? 
                             (!string.IsNullOrWhiteSpace(c.User.FirstName) || !string.IsNullOrWhiteSpace(c.User.LastName)
                                 ? $"{c.User.FirstName} {c.User.LastName}".Trim()
                                 : c.User.Email),
                Avatar = c.User.Profile?.Avatar
            },
            Content = c.Content,
            ParentCommentId = c.ParentCommentId,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Replies = (replies.ContainsKey(c.Id) ? replies[c.Id] : new List<Comment>())
                .Where(r => !r.IsDeleted)
                .Select(r => new CommentDto
                {
                    Id = r.Id,
                    User = new UserBasicDto
                    {
                        Id = r.User.Id,
                        DisplayName = r.User.Profile?.DisplayName ?? 
                                     (!string.IsNullOrWhiteSpace(r.User.FirstName) || !string.IsNullOrWhiteSpace(r.User.LastName)
                                         ? $"{r.User.FirstName} {r.User.LastName}".Trim()
                                         : r.User.Email),
                        Avatar = r.User.Profile?.Avatar
                    },
                    Content = r.Content,
                    ParentCommentId = r.ParentCommentId,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    Replies = new List<CommentDto>()
                }).ToList()
        }).ToList();
        // var ticketComments = await _context.Comments
        //     .AsNoTracking()
        //     .Include(c => c.User)
        //         .ThenInclude(u => u.Profile)
        //     .Where(c => c.CommentableType == CommentableType.Ticket && c.CommentableId == ticket.Id)
        //     .OrderByDescending(c => c.CreatedAt)
        //     .Take(20)
        //     .ToListAsync(cancellationToken);
        // 
        // // Group comments by parent
        // var topLevelComments = ticketComments.Where(c => c.ParentCommentId == null).ToList();
        // var replies = ticketComments.Where(c => c.ParentCommentId != null)
        //     .GroupBy(c => c.ParentCommentId)
        //     .ToDictionary(g => g.Key!.Value, g => g.ToList());
        // 
        // comments = topLevelComments.Select(c => new CommentDto
        // {
        //     Id = c.Id,
        //     User = new UserBasicDto
        //     {
        //         Id = c.User.Id,
        //         DisplayName = c.User.Profile?.DisplayName ?? 
        //                      (!string.IsNullOrWhiteSpace(c.User.FirstName) || !string.IsNullOrWhiteSpace(c.User.LastName)
        //                          ? $"{c.User.FirstName} {c.User.LastName}".Trim()
        //                          : c.User.Email),
        //         Avatar = c.User.Profile?.Avatar
        //     },
        //     Content = c.Content,
        //     ParentCommentId = c.ParentCommentId,
        //     CreatedAt = c.CreatedAt,
        //     UpdatedAt = c.UpdatedAt,
        //     Replies = replies.GetValueOrDefault(c.Id, new List<Comment>())
        //         .Select(r => new CommentDto
        //         {
        //             Id = r.Id,
        //             User = new UserBasicDto { ... },
        //             Content = r.Content,
        //             ParentCommentId = r.ParentCommentId,
        //             CreatedAt = r.CreatedAt,
        //             UpdatedAt = r.UpdatedAt,
        //             Replies = new List<CommentDto>()
        //         }).ToList()
        // }).ToList();

        // Apply blurring logic to entries: hide selection/analysis if expert win rate >= 80% and user not subscribed
        bool shouldBlur = winRate.HasValue && winRate.Value >= 80.0m && !isSubscribed;

        // Map entries with blurring logic
        var entryDtos = ticket.Entries
            .OrderBy(e => e.DisplayOrder)
            .ThenBy(e => e.CreatedAt)
            .Select(entry => new TicketEntryDetailDto
            {
                Id = entry.Id,
                SportEventId = entry.SportEventId,
                SportEvent = new SportEventBasicDto
                {
                    Id = entry.SportEvent.Id,
                    Sport = entry.Sport,
                    League = entry.League,
                    HomeTeam = entry.HomeTeam,
                    AwayTeam = entry.AwayTeam,
                    HomeTeamLogo = entry.SportEvent.HomeTeamLogo,
                    AwayTeamLogo = entry.SportEvent.AwayTeamLogo,
                    StartTime = entry.EventStartTime,
                    Status = entry.SportEvent.Status.ToString()
                },
                Market = entry.Market,
                Selection = shouldBlur ? "***" : entry.Selection,
                Odds = entry.Odds,
                Handicap = entry.Handicap,
                Analysis = shouldBlur ? null : entry.Analysis,
                Status = entry.Status.ToString(),
                Result = entry.Result?.ToString(),
                ResultNotes = entry.ResultNotes,
                EventStartTime = entry.EventStartTime,
                SettledAt = entry.SettledAt,
                DisplayOrder = entry.DisplayOrder
            }).ToList();

        // Map to DTO
        var dto = new TicketDetailDto
        {
            Id = ticket.Id,
            Expert = new ExpertDetailDto
            {
                Id = expert.Id,
                DisplayName = expert.Profile?.DisplayName ??
                             (!string.IsNullOrWhiteSpace(expert.FirstName) || !string.IsNullOrWhiteSpace(expert.LastName)
                                 ? $"{expert.FirstName} {expert.LastName}".Trim()
                                 : expert.Email),
                Avatar = expert.Profile?.Avatar,
                Bio = bio,
                Specialization = specialization,
                Tier = tier,
                IsVerified = isVerified,
                VerifiedAt = verifiedAt,
                Statistics = expertStats,
                SubscriberCount = subscriberCount
            },
            Title = ticket.Title,
            Description = ticket.Description,
            Type = ticket.Type.ToString(),
            Status = ticket.Status.ToString(),
            TotalOdds = ticket.TotalOdds,
            Stake = ticket.Stake,
            PotentialReturn = ticket.PotentialReturn,
            Visibility = ticket.Visibility.ToString(),
            Result = ticket.Result?.ToString(),
            FinalOdds = ticket.FinalOdds,
            SettlementNotes = ticket.SettlementNotes,
            ViewCount = ticket.ViewCount,
            FollowerCount = ticket.FollowerCount,
            UpvoteCount = ticket.UpvoteCount,
            DownvoteCount = ticket.DownvoteCount,
            CommentCount = ticket.CommentCount,
            CreatedAt = ticket.CreatedAt,
            PublishedAt = ticket.PublishedAt,
            SettledAt = ticket.SettledAt,
            ExpiresAt = ticket.ExpiresAt,
            Entries = entryDtos,
            Comments = comments,
            UserVote = userVote,
            IsFollowing = isFollowing
        };

        return dto;
    }
}


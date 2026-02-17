using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Rebet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Rebet.Infrastructure.Repositories;

public class ExpertRepository : Repository<Expert>, IExpertRepository
{
    public ExpertRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DisplayNameExistsAsync(string displayName, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(e => e.DisplayName == displayName && !e.IsDeleted, cancellationToken);
    }

    public async Task<Expert?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.Statistics)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.UserId == userId && !e.IsDeleted, cancellationToken);
    }

    public override async Task<Expert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.Statistics)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }

    public async Task<PagedResult<ExpertListDto>> GetLeaderboardAsync(
        string sortBy,
        string? specialization,
        decimal? minWinRate,
        int? tier,
        int page,
        int pageSize,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        // Build query with AsNoTracking for read-only performance
        // Query experts with statistics, user profile, and subscription count
        var query = _dbSet
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.Status == ExpertStatus.Active)
            .Include(e => e.Statistics)
            .Include(e => e.User)
                .ThenInclude(u => u.Profile)
            .AsQueryable();

        // Filter by specialization if provided
        if (!string.IsNullOrWhiteSpace(specialization))
        {
            query = query.Where(e => e.Specialization != null && 
                e.Specialization.ToLower() == specialization.ToLower());
        }

        // Filter by minimum win rate if provided
        if (minWinRate.HasValue)
        {
            query = query.Where(e => e.Statistics != null && 
                e.Statistics.WinRate >= minWinRate.Value);
        }

        // Filter by tier if provided
        if (tier.HasValue && Enum.IsDefined(typeof(ExpertTier), tier.Value))
        {
            var expertTier = (ExpertTier)tier.Value;
            query = query.Where(e => e.Tier == expertTier);
        }

        // Apply sorting
        query = sortBy.ToLower() switch
        {
            "winrate" => query.OrderByDescending(e => e.Statistics != null ? e.Statistics.WinRate : 0)
                             .ThenByDescending(e => e.UpvoteCount),
            "roi" => query.OrderByDescending(e => e.Statistics != null ? e.Statistics.ROI : 0)
                         .ThenByDescending(e => e.Statistics != null ? e.Statistics.WinRate : 0),
            "upvotes" => query.OrderByDescending(e => e.UpvoteCount)
                             .ThenByDescending(e => e.Statistics != null ? e.Statistics.WinRate : 0),
            "subscribers" => query.OrderByDescending(e => e.Statistics != null ? e.Statistics.TotalSubscribers : 0)
                                 .ThenByDescending(e => e.UpvoteCount),
            _ => query.OrderByDescending(e => e.Statistics != null ? e.Statistics.WinRate : 0)
                     .ThenByDescending(e => e.UpvoteCount)
        };

        // Get total count before pagination
        var totalItems = await query.CountAsync(cancellationToken);

        // Apply pagination
        var experts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Get subscription counts for all experts
        var expertIds = experts.Select(e => e.Id).ToList();
        var subscriptionCounts = await _context.Set<Subscription>()
            .AsNoTracking()
            .Where(s => expertIds.Contains(s.ExpertId) && 
                       s.Status == SubscriptionStatus.Active && 
                       !s.IsDeleted)
            .GroupBy(s => s.ExpertId)
            .Select(g => new { ExpertId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ExpertId, x => x.Count, cancellationToken);

        // Get user's votes if authenticated
        Dictionary<Guid, string>? userVotes = null;
        Dictionary<Guid, bool>? userSubscriptions = null;
        
        if (userId.HasValue)
        {
            // Get user's votes on experts
            // TODO: Uncomment when Vote entity is added to DbContext
            // var votes = await _context.Set<Vote>()
            //     .AsNoTracking()
            //     .Where(v => v.UserId == userId.Value && 
            //                 v.VoteableType == VoteableType.Expert &&
            //                 expertIds.Contains(v.VoteableId))
            //     .ToListAsync(cancellationToken);
            // userVotes = votes.ToDictionary(
            //     v => v.VoteableId,
            //     v => v.Type == VoteType.Upvote ? "upvote" : "downvote");

            // Get user's subscriptions
            var subscriptions = await _context.Set<Subscription>()
                .AsNoTracking()
                .Where(s => s.UserId == userId.Value && 
                           expertIds.Contains(s.ExpertId) &&
                           s.Status == SubscriptionStatus.Active &&
                           !s.IsDeleted)
                .ToListAsync(cancellationToken);
            userSubscriptions = subscriptions.ToDictionary(s => s.ExpertId, s => true);
        }

        // Map to DTOs
        var expertDtos = experts.Select(e =>
        {
            var subscriptionCount = subscriptionCounts.GetValueOrDefault(e.Id, 0);
            
            // Update subscription count in statistics if available
            if (e.Statistics != null)
            {
                subscriptionCount = e.Statistics.TotalSubscribers > 0 
                    ? e.Statistics.TotalSubscribers 
                    : subscriptionCount;
            }

            return new ExpertListDto
            {
                Id = e.Id,
                DisplayName = e.DisplayName,
                Avatar = e.User.Profile?.Avatar,
                Bio = e.Bio,
                Specialization = e.Specialization,
                Tier = e.Tier.ToString(),
                IsVerified = e.IsVerified,
                Statistics = e.Statistics != null ? new ExpertStatisticsListDto
                {
                    TotalPositions = e.Statistics.TotalPositions,
                    TotalTickets = e.Statistics.TotalTickets,
                    WinRate = e.Statistics.WinRate,
                    ROI = e.Statistics.ROI,
                    CurrentStreak = e.Statistics.CurrentStreak,
                    Last30DaysWinRate = e.Statistics.Last30DaysWinRate,
                    AverageOdds = e.Statistics.AverageOdds
                } : new ExpertStatisticsListDto(),
                UpvoteCount = e.UpvoteCount,
                DownvoteCount = e.DownvoteCount,
                SubscriberCount = subscriptionCount,
                UserVote = userId.HasValue && userVotes != null && userVotes.TryGetValue(e.Id, out var vote) 
                    ? vote 
                    : null,
                IsSubscribed = userId.HasValue && userSubscriptions != null && userSubscriptions.ContainsKey(e.Id)
                    ? userSubscriptions[e.Id]
                    : null
            };
        }).ToList();

        // Calculate pagination metadata
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedResult<ExpertListDto>
        {
            Data = expertDtos,
            Pagination = new PagedResult<ExpertListDto>.PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            }
        };
    }

    public async Task<PagedResult<ExpertListDto>> SearchAsync(
        string searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new PagedResult<ExpertListDto>
            {
                Data = new List<ExpertListDto>(),
                Pagination = new PagedResult<ExpertListDto>.PaginationMetadata
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = 0,
                    TotalPages = 0
                }
            };
        }

        var lowerSearchTerm = searchTerm.ToLower();

        // Build query with AsNoTracking for read-only performance
        var query = _dbSet
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.Status == ExpertStatus.Active)
            .Where(e => e.DisplayName.ToLower().Contains(lowerSearchTerm) ||
                       (e.Bio != null && e.Bio.ToLower().Contains(lowerSearchTerm)) ||
                       (e.Specialization != null && e.Specialization.ToLower().Contains(lowerSearchTerm)))
            .Include(e => e.Statistics)
            .Include(e => e.User)
                .ThenInclude(u => u.Profile)
            .OrderByDescending(e => e.Statistics != null ? e.Statistics.WinRate : 0)
            .ThenByDescending(e => e.UpvoteCount);

        // Get total count before pagination
        var totalItems = await query.CountAsync(cancellationToken);

        // Apply pagination
        var experts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Get subscription counts for all experts
        var expertIds = experts.Select(e => e.Id).ToList();
        var subscriptionCounts = await _context.Set<Subscription>()
            .AsNoTracking()
            .Where(s => expertIds.Contains(s.ExpertId) &&
                       s.Status == SubscriptionStatus.Active &&
                       !s.IsDeleted)
            .GroupBy(s => s.ExpertId)
            .Select(g => new { ExpertId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ExpertId, x => x.Count, cancellationToken);

        // Map to DTOs
        var expertDtos = experts.Select(e =>
        {
            var subscriptionCount = subscriptionCounts.GetValueOrDefault(e.Id, 0);

            // Update subscription count in statistics if available
            if (e.Statistics != null)
            {
                subscriptionCount = e.Statistics.TotalSubscribers > 0
                    ? e.Statistics.TotalSubscribers
                    : subscriptionCount;
            }

            return new ExpertListDto
            {
                Id = e.Id,
                DisplayName = e.DisplayName,
                Avatar = e.User.Profile?.Avatar,
                Bio = e.Bio,
                Specialization = e.Specialization,
                Tier = e.Tier.ToString(),
                IsVerified = e.IsVerified,
                Statistics = e.Statistics != null ? new ExpertStatisticsListDto
                {
                    TotalPositions = e.Statistics.TotalPositions,
                    TotalTickets = e.Statistics.TotalTickets,
                    WinRate = e.Statistics.WinRate,
                    ROI = e.Statistics.ROI,
                    CurrentStreak = e.Statistics.CurrentStreak,
                    Last30DaysWinRate = e.Statistics.Last30DaysWinRate,
                    AverageOdds = e.Statistics.AverageOdds
                } : new ExpertStatisticsListDto(),
                UpvoteCount = e.UpvoteCount,
                DownvoteCount = e.DownvoteCount,
                SubscriberCount = subscriptionCount,
                UserVote = null,
                IsSubscribed = null
            };
        }).ToList();

        // Calculate pagination metadata
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedResult<ExpertListDto>
        {
            Data = expertDtos,
            Pagination = new PagedResult<ExpertListDto>.PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            }
        };
    }
}


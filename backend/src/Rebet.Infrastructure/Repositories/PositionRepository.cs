using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Rebet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Rebet.Infrastructure.Repositories;

public class PositionRepository : Repository<Position>, IPositionRepository
{
    public PositionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public override async Task<Position?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Positions
            .Where(p => !p.IsDeleted)
            .Include(p => p.Creator)
                .ThenInclude(c => c!.Profile)
            .Include(p => p.Creator)
                .ThenInclude(c => c!.Expert)
                    .ThenInclude(e => e!.Statistics)
            .Include(p => p.SportEvent)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PagedResult<PositionListDto>> GetTopPositionsAsync(
        UserRole creatorType,
        string? sport,
        PositionStatus? status,
        string sortBy,
        int page,
        int pageSize,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        // Build query with AsNoTracking for read-only
        var query = _dbSet
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Where(p => p.CreatorType == creatorType)
            .Include(p => p.Creator)
                .ThenInclude(c => c!.Profile)
            .Include(p => p.Creator)
                .ThenInclude(c => c!.Expert)
                    .ThenInclude(e => e!.Statistics)
            .Include(p => p.SportEvent)
            .AsQueryable();

        // Filter by sport if provided
        if (!string.IsNullOrWhiteSpace(sport))
        {
            query = query.Where(p => p.SportEvent != null && p.SportEvent.Sport.ToLower() == sport.ToLower());
        }

        // Filter by status if provided
        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        // Apply sorting
        query = sortBy.ToLower() switch
        {
            "upvotes" => query.OrderByDescending(p => p.UpvoteCount).ThenByDescending(p => p.CreatedAt),
            "created" => query.OrderByDescending(p => p.CreatedAt),
            "odds" => query.OrderByDescending(p => p.Odds).ThenByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.UpvoteCount).ThenByDescending(p => p.CreatedAt)
        };

        // Get total count before pagination
        var totalItems = await query.CountAsync(cancellationToken);

        // Apply pagination
        var positions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Get user votes if userId provided
        var userVotes = new Dictionary<Guid, VoteType?>();
        if (userId.HasValue)
        {
            var votes = await _context.Votes
                .AsNoTracking()
                .Where(v => v.UserId == userId.Value
                         && v.VoteableType == VoteableType.Position
                         && !v.IsDeleted
                         && positions.Select(p => p.Id).Contains(v.VoteableId))
                .ToListAsync(cancellationToken);
            
            foreach (var vote in votes)
            {
                userVotes[vote.VoteableId] = vote.Type;
            }
        }

        // Map to DTOs
        var positionDtos = positions.Select(p => 
        {
            string? userVote = null;
            if (userId.HasValue && userVotes.TryGetValue(p.Id, out var voteType))
            {
                userVote = voteType == VoteType.Upvote ? "upvote" : "downvote";
            }

            return new PositionListDto
            {
                Id = p.Id,
                CreatorId = p.CreatorId,
                CreatorName = p.Creator.Profile?.DisplayName ?? 
                             (!string.IsNullOrWhiteSpace(p.Creator.FirstName) || !string.IsNullOrWhiteSpace(p.Creator.LastName)
                                 ? $"{p.Creator.FirstName} {p.Creator.LastName}".Trim()
                                 : p.Creator.Email),
                CreatorAvatar = p.Creator.Profile?.Avatar,
                IsExpert = p.CreatorType == UserRole.Expert,
                SportEvent = new SportEventListDto
                {
                    Id = p.SportEvent.Id,
                    HomeTeam = p.SportEvent.HomeTeam,
                    AwayTeam = p.SportEvent.AwayTeam,
                    League = p.SportEvent.League,
                    StartTime = p.SportEvent.StartTimeUtc
                },
                Market = p.Market,
                Selection = p.Selection,
                Odds = p.Odds,
                Analysis = p.Analysis,
                Status = p.Status.ToString(),
                UpvoteCount = p.UpvoteCount,
                DownvoteCount = p.DownvoteCount,
                VoterCount = p.VoterCount,
                PredictionPercentage = p.PredictionPercentage,
                ViewCount = p.ViewCount,
                CreatedAt = p.CreatedAt,
                UserVote = userVote
            };
        }).ToList();

        // Calculate total pages
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedResult<PositionListDto>
        {
            Data = positionDtos,
            Pagination = new PagedResult<PositionListDto>.PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            }
        };
    }

    public async Task<IEnumerable<Position>> GetByCreatorIdAsync(
        Guid creatorId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.CreatorId == creatorId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Position>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.SportEventId == eventId)
            .Include(p => p.Creator)
                .ThenInclude(c => c!.Profile)
            .Include(p => p.SportEvent)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Position>> GetWonPositionsAsync(
        string? sport,
        DateTime? startDate,
        decimal? minOdds,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(p => !p.IsDeleted 
                     && p.CreatorType == UserRole.Expert
                     && p.Status == PositionStatus.Won)
            .Include(p => p.Creator)
                .ThenInclude(u => u.Profile)
            .Include(p => p.SportEvent)
            .AsQueryable();

        // Filter by sport if provided
        if (!string.IsNullOrWhiteSpace(sport))
        {
            query = query.Where(p => p.SportEvent != null && p.SportEvent.Sport.ToLower() == sport.ToLower());
        }

        // Filter by date range if provided
        if (startDate.HasValue)
        {
            query = query.Where(p => p.SettledAt.HasValue && p.SettledAt.Value >= startDate.Value);
        }

        // Filter by minimum odds if provided
        if (minOdds.HasValue)
        {
            query = query.Where(p => p.Odds >= minOdds.Value);
        }

        // Get total count before pagination
        var totalItems = await query.CountAsync(cancellationToken);

        // Apply pagination
        var positions = await query
            .OrderByDescending(p => p.SettledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedResult<Position>
        {
            Data = positions,
            Pagination = new PagedResult<Position>.PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            }
        };
    }
}


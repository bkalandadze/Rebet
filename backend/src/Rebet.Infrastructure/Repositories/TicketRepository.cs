using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Rebet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Rebet.Infrastructure.Repositories;

public class TicketRepository : Repository<Ticket>, ITicketRepository
{
    public TicketRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await transaction.RollbackAsync(cancellationToken);
        }
    }

    public async Task<TicketEntry> AddEntryAsync(TicketEntry entry, CancellationToken cancellationToken = default)
    {
        await _context.TicketEntries.AddAsync(entry, cancellationToken);
        return entry;
    }

    public override async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => !t.IsDeleted)
            .Include(t => t.Entries)
                .ThenInclude(e => e.SportEvent)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Ticket?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => !t.IsDeleted)
            .Include(t => t.Entries)
                .ThenInclude(e => e.SportEvent)
            // TODO: Include Votes when Vote entity is added
            // .Include(t => t.Votes)
            // TODO: Include Comments when Comment entity is added
            // .Include(t => t.Comments)
            //     .ThenInclude(c => c.User)
            //         .ThenInclude(u => u.Profile)
            // .Include(t => t.Comments)
            //     .ThenInclude(c => c.Replies)
            //         .ThenInclude(r => r.User)
            //             .ThenInclude(u => u.Profile)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<PagedResult<TicketListDto>> GetTopTicketsAsync(
        UserRole creatorType,
        string? sport,
        TicketStatus? status,
        decimal? minOdds,
        string sortBy,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Build base query - join tickets with users to filter by role
        var query = from ticket in _dbSet
                    join user in _context.Users on ticket.ExpertId equals user.Id
                    where !ticket.IsDeleted && !user.IsDeleted && user.Role == creatorType
                    select ticket;

        // Filter by sport if provided (through ticket entries)
        if (!string.IsNullOrWhiteSpace(sport))
        {
            query = query.Where(t => t.Entries.Any(e => e.Sport.ToLower() == sport.ToLower()));
        }

        // Filter by status if provided
        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        // Filter by minimum odds if provided
        if (minOdds.HasValue)
        {
            query = query.Where(t => t.TotalOdds >= minOdds.Value);
        }

        // Apply sorting
        query = sortBy.ToLower() switch
        {
            "odds" => query.OrderByDescending(t => t.TotalOdds).ThenByDescending(t => t.CreatedAt),
            "upvotes" => query.OrderByDescending(t => t.UpvoteCount).ThenByDescending(t => t.CreatedAt),
            "created" => query.OrderByDescending(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.TotalOdds).ThenByDescending(t => t.CreatedAt)
        };

        // Get total count before pagination
        var totalItems = await query.CountAsync(cancellationToken);

        // Apply pagination and load related entities
        var tickets = await query
            .AsNoTracking()
            .Include(t => t.Entries)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Get expert users for the tickets
        var expertIds = tickets.Select(t => t.ExpertId).Distinct().ToList();
        var experts = await _context.Users
            .AsNoTracking()
            .Where(u => expertIds.Contains(u.Id) && !u.IsDeleted)
            .Include(u => u.Profile)
            .ToListAsync(cancellationToken);

        var expertDict = experts.ToDictionary(u => u.Id, u => u);

        // Map to DTOs
        var ticketDtos = tickets.Select(t =>
        {
            var expert = expertDict.GetValueOrDefault(t.ExpertId);
            return new TicketListDto
            {
                Id = t.Id,
                Expert = new ExpertInfoDto
                {
                    Id = expert?.Id ?? t.ExpertId,
                    DisplayName = expert?.Profile?.DisplayName ??
                                 (!string.IsNullOrWhiteSpace(expert?.FirstName) || !string.IsNullOrWhiteSpace(expert?.LastName)
                                     ? $"{expert?.FirstName} {expert?.LastName}".Trim()
                                     : expert?.Email ?? "Unknown"),
                    Avatar = expert?.Profile?.Avatar,
                    WinRate = null, // TODO: Implement when ExpertStatistics is available
                    Tier = null // TODO: Implement when Expert entity is available
                },
                Title = t.Title,
                Description = t.Description,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                TotalOdds = t.TotalOdds,
                PotentialReturn = t.PotentialReturn,
                EntryCount = t.Entries.Count,
                UpvoteCount = t.UpvoteCount,
                DownvoteCount = t.DownvoteCount,
                CommentCount = t.CommentCount,
                ViewCount = t.ViewCount,
                FollowerCount = t.FollowerCount,
                CreatedAt = t.CreatedAt,
                ExpiresAt = t.ExpiresAt,
                UserVote = null, // TODO: Implement user vote lookup if authenticated user context is available
                IsFollowing = null // TODO: Implement follow lookup if authenticated user context is available
            };
        }).ToList();

        // Calculate total pages
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedResult<TicketListDto>
        {
            Data = ticketDtos,
            Pagination = new PagedResult<TicketListDto>.PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            }
        };
    }

    public async Task<IEnumerable<Ticket>> GetByExpertIdAsync(
        Guid expertId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.ExpertId == expertId)
            .Include(t => t.Entries)
                .ThenInclude(e => e.SportEvent)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTicketCountForEventAsync(
        Guid sportEventId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<TicketEntry>()
            .Where(te => te.SportEventId == sportEventId && !te.IsDeleted)
            .Select(te => te.TicketId)
            .Distinct()
            .CountAsync(cancellationToken);
    }
}


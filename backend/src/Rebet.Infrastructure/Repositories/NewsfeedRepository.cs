using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Rebet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Rebet.Infrastructure.Repositories;

public class NewsfeedRepository : Repository<NewsfeedItem>, INewsfeedRepository
{
    public NewsfeedRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<NewsfeedItemDto>> GetNewsfeedAsync(
        string type,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Build query with eager loading to avoid N+1 queries
        var query = _dbSet
            .AsNoTracking()
            .Include(n => n.Expert)
                .ThenInclude(e => e!.User)
                    .ThenInclude(u => u!.Profile)
            .Include(n => n.Position)
            .Include(n => n.Ticket)
            .AsQueryable();

        // Filter by type if not "all"
        if (!string.IsNullOrWhiteSpace(type) && type!.ToLower() != "all")
        {
            var newsfeedTypes = type.ToLowerInvariant() switch
            {
                "expert" => new[] { NewsfeedType.ExpertAchievement, NewsfeedType.NewExpert, NewsfeedType.TopListChange },
                "position" => new[] { NewsfeedType.NewPosition, NewsfeedType.SuccessfulPrediction },
                "ticket" => new[] { NewsfeedType.TicketWon },
                _ => throw new ArgumentException($"Invalid type: {type}. Must be 'all', 'expert', 'position', or 'ticket'.")
            };

            query = query.Where(n => newsfeedTypes.Contains(n.Type));
        }

        // Order by created_at DESC
        query = query.OrderByDescending(n => n.CreatedAt);

        // Get total count before pagination
        var totalItems = await query.CountAsync(cancellationToken);

        // Apply pagination
        var newsfeedItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var newsfeedItemDtos = newsfeedItems.Select(n => new NewsfeedItemDto
        {
            Id = n.Id,
            Type = n.Type.ToString(),
            Title = n.Title,
            Description = n.Description,
            ActionUrl = n.ActionUrl,
            Expert = n.Expert != null ? new ExpertBasicDto
            {
                DisplayName = !string.IsNullOrWhiteSpace(n.Expert.DisplayName) 
                    ? n.Expert.DisplayName
                    : n.Expert.User?.Profile?.DisplayName ??
                      (!string.IsNullOrWhiteSpace(n.Expert.User?.FirstName) || !string.IsNullOrWhiteSpace(n.Expert.User?.LastName)
                          ? $"{n.Expert.User?.FirstName} {n.Expert.User?.LastName}".Trim()
                          : n.Expert.User?.Email ?? "Unknown"),
                Avatar = n.Expert.User?.Profile?.Avatar
            } : null,
            CreatedAt = n.CreatedAt
        }).ToList();

        // Calculate pagination metadata
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedResult<NewsfeedItemDto>
        {
            Data = newsfeedItemDtos,
            Pagination = new PagedResult<NewsfeedItemDto>.PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            }
        };
    }
}


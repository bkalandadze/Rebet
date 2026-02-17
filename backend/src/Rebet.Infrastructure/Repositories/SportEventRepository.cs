using Rebet.Application.DTOs;
using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Rebet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Rebet.Infrastructure.Repositories;

public class SportEventRepository : Repository<SportEvent>, ISportEventRepository
{
    public SportEventRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }

    public async Task<bool> IsScheduledAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(e => e.Id == id 
                && !e.IsDeleted 
                && e.Status == EventStatus.Scheduled, cancellationToken);
    }

    public async Task<PagedResult<EventListDto>> GetAllEventsAsync(
        string? sport,
        string? league,
        DateTime? date,
        EventStatus? status,
        bool? hasExpertPredictions,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        // Filter by sport
        if (!string.IsNullOrWhiteSpace(sport))
        {
            query = query.Where(e => e.Sport.ToLower() == sport.ToLower());
        }

        // Filter by league
        if (!string.IsNullOrWhiteSpace(league))
        {
            query = query.Where(e => e.League.ToLower().Contains(league.ToLower()));
        }

        // Filter by date
        if (date.HasValue)
        {
            var startOfDay = date.Value.Date;
            var endOfDay = startOfDay.AddDays(1);
            query = query.Where(e => e.StartTimeUtc >= startOfDay && e.StartTimeUtc < endOfDay);
        }

        // Filter by status
        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        // Filter by expert predictions
        if (hasExpertPredictions.HasValue && hasExpertPredictions.Value)
        {
            query = query.Where(e => e.Positions.Any(p => !p.IsDeleted && p.CreatorType == UserRole.Expert));
        }

        // Get total count before pagination
        var totalItems = await query.CountAsync(cancellationToken);

        // Apply pagination
        var events = await query
            .Include(e => e.Positions.Where(p => !p.IsDeleted))
            .OrderBy(e => e.StartTimeUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Get event IDs for counting positions and tickets
        var eventIds = events.Select(e => e.Id).ToList();
        
        // Count positions per event
        var positionCounts = await _context.Set<Position>()
            .Where(p => eventIds.Contains(p.SportEventId) && !p.IsDeleted)
            .GroupBy(p => p.SportEventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count, cancellationToken);

        // Count tickets per event (through ticket entries)
        var ticketCounts = await _context.Set<TicketEntry>()
            .Where(te => eventIds.Contains(te.SportEventId))
            .GroupBy(te => te.SportEventId)
            .Select(g => new { EventId = g.Key, Count = g.Select(te => te.TicketId).Distinct().Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count, cancellationToken);

        // Calculate sentiment for events (simplified - count positions by selection)
        var positions = await _context.Set<Position>()
            .Where(p => eventIds.Contains(p.SportEventId) && !p.IsDeleted)
            .Select(p => new { p.SportEventId, p.Market, p.Selection, p.CreatorType, p.CreatorId })
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var eventDtos = events.Select(e =>
        {
            var eventPositions = positions.Where(p => p.SportEventId == e.Id).ToList();
            var expertPositions = eventPositions.Where(p => p.CreatorType == UserRole.Expert).ToList();
            var userPositions = eventPositions.Where(p => p.CreatorType == UserRole.User).ToList();

            // Convert to Position-like objects for sentiment calculation
            var expertPos = expertPositions.Select(p => new Position 
            { 
                Market = p.Market, 
                Selection = p.Selection, 
                CreatorId = p.CreatorId 
            }).ToList();
            
            var userPos = userPositions.Select(p => new Position 
            { 
                Market = p.Market, 
                Selection = p.Selection, 
                CreatorId = p.CreatorId 
            }).ToList();

            return new EventListDto
            {
                Id = e.Id,
                Sport = e.Sport,
                League = e.League,
                HomeTeam = e.HomeTeam,
                AwayTeam = e.AwayTeam,
                HomeTeamLogo = e.HomeTeamLogo,
                AwayTeamLogo = e.AwayTeamLogo,
                StartTime = e.StartTimeUtc,
                Status = e.Status.ToString(),
                Odds = new EventOddsDto
                {
                    HomeWin = e.HomeWinOdds,
                    Draw = e.DrawOdds,
                    AwayWin = e.AwayWinOdds
                },
                Sentiment = expertPos.Count > 0 || userPos.Count > 0 ? new EventSentimentDto
                {
                    Expert = expertPos.Count > 0 ? CalculateExpertSentiment(expertPos) : null,
                    User = userPos.Count > 0 ? CalculateUserSentiment(userPos) : null
                } : null,
                PositionCount = positionCounts.GetValueOrDefault(e.Id, 0),
                TicketCount = ticketCounts.GetValueOrDefault(e.Id, 0)
            };
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedResult<EventListDto>
        {
            Data = eventDtos,
            Pagination = new PagedResult<EventListDto>.PaginationMetadata
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            }
        };
    }

    public async Task<EventDetailDto?> GetEventDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sportEvent = await _dbSet
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

        if (sportEvent == null)
        {
            return null;
        }

        // Get positions for this event
        var positions = await _context.Set<Position>()
            .Where(p => p.SportEventId == id && !p.IsDeleted)
            .Include(p => p.Creator)
                .ThenInclude(u => u.Profile)
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        // Get tickets for this event (through ticket entries)
        var ticketIds = await _context.Set<TicketEntry>()
            .Where(te => te.SportEventId == id)
            .Select(te => te.TicketId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Calculate sentiment
        var expertPositions = positions.Where(p => p.CreatorType == UserRole.Expert).ToList();
        var userPositions = positions.Where(p => p.CreatorType == UserRole.User).ToList();

        // Get top experts (experts with most positions on this event)
        var topExpertIds = expertPositions
            .GroupBy(p => p.CreatorId)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key)
            .ToList();

        var topExperts = await _context.Set<Expert>()
            .Where(ex => topExpertIds.Contains(ex.UserId) && !ex.IsDeleted)
            .Include(ex => ex.Statistics)
            .Include(ex => ex.User)
                .ThenInclude(u => u.Profile)
            .ToListAsync(cancellationToken);

        // Map positions to DTOs
        var positionDtos = positions.Select(p => new PositionListDto
        {
            Id = p.Id,
            CreatorId = p.CreatorId,
            CreatorName = p.Creator.Profile?.DisplayName ?? p.Creator.Email,
            CreatorAvatar = p.Creator.Profile?.Avatar,
            IsExpert = p.CreatorType == UserRole.Expert,
            SportEvent = new SportEventListDto
            {
                Id = sportEvent.Id,
                HomeTeam = sportEvent.HomeTeam,
                AwayTeam = sportEvent.AwayTeam,
                League = sportEvent.League,
                StartTime = sportEvent.StartTimeUtc
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
            UserVote = null
        }).ToList();

        // Map experts to DTOs
        var expertDtos = topExperts.Select(ex => new ExpertListDto
        {
            Id = ex.Id,
            DisplayName = ex.DisplayName,
            Avatar = ex.User.Profile?.Avatar,
            Bio = ex.Bio,
            Specialization = ex.Specialization,
            Tier = ex.Tier.ToString(),
            IsVerified = ex.IsVerified,
            Statistics = ex.Statistics != null ? new ExpertStatisticsListDto
            {
                TotalPositions = ex.Statistics.TotalPositions,
                TotalTickets = ex.Statistics.TotalTickets,
                WinRate = ex.Statistics.WinRate,
                ROI = ex.Statistics.ROI,
                CurrentStreak = ex.Statistics.CurrentStreak,
                Last30DaysWinRate = ex.Statistics.Last30DaysWinRate,
                AverageOdds = ex.Statistics.AverageOdds
            } : new ExpertStatisticsListDto(),
            UpvoteCount = ex.UpvoteCount,
            DownvoteCount = ex.DownvoteCount,
            SubscriberCount = ex.Statistics?.TotalSubscribers ?? 0,
            UserVote = null,
            IsSubscribed = null
        }).ToList();

        return new EventDetailDto
        {
            Id = sportEvent.Id,
            Sport = sportEvent.Sport,
            League = sportEvent.League,
            HomeTeam = sportEvent.HomeTeam,
            AwayTeam = sportEvent.AwayTeam,
            HomeTeamLogo = sportEvent.HomeTeamLogo,
            AwayTeamLogo = sportEvent.AwayTeamLogo,
            StartTime = sportEvent.StartTimeUtc,
            Status = sportEvent.Status.ToString(),
            Venue = null, // TODO: Add venue to SportEvent entity if needed
            Markets = new EventMarketsDto
            {
                MatchResult = new EventOddsDto
                {
                    HomeWin = sportEvent.HomeWinOdds,
                    Draw = sportEvent.DrawOdds,
                    AwayWin = sportEvent.AwayWinOdds
                },
                OverUnder = sportEvent.Over25Odds.HasValue || sportEvent.Under25Odds.HasValue
                    ? new List<OverUnderDto>
                    {
                        new OverUnderDto
                        {
                            Line = 2.5m,
                            Over = sportEvent.Over25Odds ?? 0,
                            Under = sportEvent.Under25Odds ?? 0
                        }
                    }
                    : null,
                BothTeamsScore = null // TODO: Add if available in entity
            },
            Sentiment = expertPositions.Count > 0 || userPositions.Count > 0
                ? new EventSentimentDto
                {
                    Expert = expertPositions.Count > 0
                        ? CalculateExpertSentiment(expertPositions)
                        : null,
                    User = userPositions.Count > 0
                        ? CalculateUserSentiment(userPositions)
                        : null
                }
                : null,
            Positions = positionDtos,
            TopExperts = expertDtos
        };
    }

    public async Task<EventDetailDto?> GetTopGameOfDayAsync(string? sport, DateTime? date, CancellationToken cancellationToken = default)
    {
        var targetDate = date ?? DateTime.UtcNow.Date;
        var startOfDay = targetDate;
        var endOfDay = startOfDay.AddDays(1);

        var query = _dbSet
            .Where(e => e.StartTimeUtc >= startOfDay 
                     && e.StartTimeUtc < endOfDay 
                     && !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(sport))
        {
            query = query.Where(e => e.Sport.ToLower() == sport.ToLower());
        }

        // Find event with most positions
        var eventWithMostPositions = await query
            .Select(e => new
            {
                Event = e,
                PositionCount = e.Positions.Count(p => !p.IsDeleted)
            })
            .OrderByDescending(x => x.PositionCount)
            .FirstOrDefaultAsync(cancellationToken);

        if (eventWithMostPositions == null || eventWithMostPositions.PositionCount == 0)
        {
            return null;
        }

        // Use GetEventDetailAsync to get full details
        return await GetEventDetailAsync(eventWithMostPositions.Event.Id, cancellationToken);
    }

    private SentimentBreakdownDto CalculateExpertSentiment(List<Position> positions)
    {
        var totalExperts = positions.Select(p => p.CreatorId).Distinct().Count();
        if (totalExperts == 0)
        {
            return new SentimentBreakdownDto
            {
                HomeWin = 0,
                Draw = 0,
                AwayWin = 0,
                TotalExperts = 0
            };
        }

        // Count positions by selection type for "Match Result" market
        var homeWinCount = positions.Count(p => 
            p.Market == "Match Result" && 
            (p.Selection.Contains("Home", StringComparison.OrdinalIgnoreCase) || 
             p.Selection.Contains("Home Win", StringComparison.OrdinalIgnoreCase) ||
             p.Selection == "1"));
        
        var drawCount = positions.Count(p => 
            p.Market == "Match Result" && 
            (p.Selection.Contains("Draw", StringComparison.OrdinalIgnoreCase) || 
             p.Selection == "X"));
        
        var awayWinCount = positions.Count(p => 
            p.Market == "Match Result" && 
            (p.Selection.Contains("Away", StringComparison.OrdinalIgnoreCase) || 
             p.Selection.Contains("Away Win", StringComparison.OrdinalIgnoreCase) ||
             p.Selection == "2"));

        var totalPositions = positions.Count(p => p.Market == "Match Result");
        
        return new SentimentBreakdownDto
        {
            HomeWin = totalPositions > 0 ? (int)((double)homeWinCount / totalPositions * 100) : 0,
            Draw = totalPositions > 0 ? (int)((double)drawCount / totalPositions * 100) : 0,
            AwayWin = totalPositions > 0 ? (int)((double)awayWinCount / totalPositions * 100) : 0,
            TotalExperts = totalExperts
        };
    }

    private SentimentBreakdownDto CalculateUserSentiment(List<Position> positions)
    {
        var totalVotes = positions.Count;
        if (totalVotes == 0)
        {
            return new SentimentBreakdownDto
            {
                HomeWin = 0,
                Draw = 0,
                AwayWin = 0,
                TotalVotes = 0
            };
        }

        // Count positions by selection type for "Match Result" market
        var homeWinCount = positions.Count(p => 
            p.Market == "Match Result" && 
            (p.Selection.Contains("Home", StringComparison.OrdinalIgnoreCase) || 
             p.Selection.Contains("Home Win", StringComparison.OrdinalIgnoreCase) ||
             p.Selection == "1"));
        
        var drawCount = positions.Count(p => 
            p.Market == "Match Result" && 
            (p.Selection.Contains("Draw", StringComparison.OrdinalIgnoreCase) || 
             p.Selection == "X"));
        
        var awayWinCount = positions.Count(p => 
            p.Market == "Match Result" && 
            (p.Selection.Contains("Away", StringComparison.OrdinalIgnoreCase) || 
             p.Selection.Contains("Away Win", StringComparison.OrdinalIgnoreCase) ||
             p.Selection == "2"));

        var totalMatchResultPositions = positions.Count(p => p.Market == "Match Result");
        
        return new SentimentBreakdownDto
        {
            HomeWin = totalMatchResultPositions > 0 ? (int)((double)homeWinCount / totalMatchResultPositions * 100) : 0,
            Draw = totalMatchResultPositions > 0 ? (int)((double)drawCount / totalMatchResultPositions * 100) : 0,
            AwayWin = totalMatchResultPositions > 0 ? (int)((double)awayWinCount / totalMatchResultPositions * 100) : 0,
            TotalVotes = totalVotes
        };
    }
}


using Rebet.Application.Interfaces;
using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Rebet.Infrastructure.BackgroundJobs.OddsExtractors;
using Rebet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Rebet.Infrastructure.BackgroundJobs;

public class SyncEventsJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOddsProviderService _oddsProviderService;
    private readonly ILogger<SyncEventsJob> _logger;
    private readonly IConfiguration _configuration;
    private readonly OddsExtractorFactory _oddsExtractorFactory;

    public SyncEventsJob(
        ApplicationDbContext dbContext,
        IOddsProviderService oddsProviderService,
        ILogger<SyncEventsJob> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _oddsProviderService = oddsProviderService;
        _logger = logger;
        _configuration = configuration;
        _oddsExtractorFactory = new OddsExtractorFactory();
    }

    public async Task SyncHotEvents()
    {
        try
        {
            // Check if Odds API is configured (not using placeholder)
            var baseUrl = _configuration["OddsApi:BaseUrl"] ?? string.Empty;
            if (string.IsNullOrEmpty(baseUrl) || baseUrl.Contains("example.com"))
            {
                _logger.LogDebug("Odds API not configured (using placeholder). Skipping event sync.");
                return;
            }

            _logger.LogInformation("Starting event sync job at {Time}", DateTime.UtcNow);

            // Call external odds API
            var oddsData = await _oddsProviderService.GetPrematchOddsAsync();

            // Filter events starting in next 24 hours
            var now = DateTime.UtcNow;
            var next24Hours = now.AddHours(24);
            var filteredEvents = oddsData.Events
                .Where(kvp => kvp.Value.StartTimeUtc >= now && kvp.Value.StartTimeUtc <= next24Hours)
                .ToList();

            _logger.LogInformation("Filtered {Count} events starting in next 24 hours", filteredEvents.Count);

            var eventsCreated = 0;
            var eventsUpdated = 0;
            var eventsToAdd = new List<SportEvent>();
            var eventsToUpdate = new List<SportEvent>();

            // Get existing events by external_event_id for batch lookup
            var externalEventIds = filteredEvents.Select(e => e.Key).ToList();
            var existingEvents = await _dbContext.SportEvents
                .Where(e => externalEventIds.Contains(e.ExternalEventId))
                .ToDictionaryAsync(e => e.ExternalEventId);

            // Process each event
            foreach (var (externalEventId, eventData) in filteredEvents)
            {
                try
                {
                    if (existingEvents.TryGetValue(externalEventId, out var existingEvent))
                    {
                        // Update existing event
                        UpdateEventOdds(existingEvent, eventData);
                        existingEvent.LastSyncedAt = DateTime.UtcNow;
                        eventsToUpdate.Add(existingEvent);
                        eventsUpdated++;
                    }
                    else
                    {
                        // Create new event
                        var newEvent = MapToSportEvent(eventData, externalEventId);
                        eventsToAdd.Add(newEvent);
                        eventsCreated++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event {EventId}: {Error}", externalEventId, ex.Message);
                    // Continue processing other events
                }
            }

            // Batch save all changes
            if (eventsToAdd.Any())
            {
                await _dbContext.SportEvents.AddRangeAsync(eventsToAdd);
            }

            if (eventsToUpdate.Any())
            {
                _dbContext.SportEvents.UpdateRange(eventsToUpdate);
            }

            if (eventsToAdd.Any() || eventsToUpdate.Any())
            {
                await _dbContext.SaveChangesAsync();
            }

            _logger.LogInformation(
                "Event sync completed. Created: {Created}, Updated: {Updated}",
                eventsCreated,
                eventsUpdated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SyncHotEvents job: {Error}", ex.Message);
            throw; // Re-throw to let Hangfire handle retry
        }
    }

    private SportEvent MapToSportEvent(OddsEventData eventData, string externalEventId)
    {
        var sportEvent = new SportEvent
        {
            ExternalEventId = externalEventId,
            Sport = eventData.Sport,
            League = eventData.League,
            HomeTeam = eventData.HomeTeam,
            AwayTeam = eventData.AwayTeam,
            HomeTeamLogo = eventData.HomeTeamLogo,
            AwayTeamLogo = eventData.AwayTeamLogo,
            StartTimeUtc = eventData.StartTimeUtc,
            StartTimeEpoch = eventData.StartTimeEpoch,
            Status = EventStatus.Scheduled,
            LastSyncedAt = DateTime.UtcNow
        };

        // Extract odds from markets
        _oddsExtractorFactory.ExtractAllOdds(sportEvent, eventData.Markets);

        return sportEvent;
    }

    private void UpdateEventOdds(SportEvent sportEvent, OddsEventData eventData)
    {
        // Only update odds if event is still scheduled
        if (sportEvent.Status == EventStatus.Scheduled)
        {
            _oddsExtractorFactory.ExtractAllOdds(sportEvent, eventData.Markets);
        }
    }
}


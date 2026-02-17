using Rebet.Application.Interfaces;
using Rebet.Infrastructure.Persistence;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rebet.Infrastructure.BackgroundJobs;

public static class SyncEventsJobWrapper
{
    private static IServiceProvider? _serviceProvider;

    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [AutomaticRetry(Attempts = 3)]
    public static async Task ExecuteSyncHotEvents()
    {
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider has not been initialized. Call SetServiceProvider first.");
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var oddsProviderService = scope.ServiceProvider.GetRequiredService<IOddsProviderService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SyncEventsJob>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        
        var job = new SyncEventsJob(dbContext, oddsProviderService, logger, configuration);
        await job.SyncHotEvents();
    }
}


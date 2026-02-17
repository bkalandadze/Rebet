using Rebet.Application.Interfaces;
using Rebet.Infrastructure.Persistence;
using Hangfire;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rebet.Infrastructure.BackgroundJobs;

public static class SettlePositionsJobWrapper
{
    private static IServiceProvider? _serviceProvider;

    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [AutomaticRetry(Attempts = 3)]
    public static async Task ExecuteSettlePositions()
    {
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("Service provider has not been initialized. Call SetServiceProvider first.");
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var expertStatisticsService = scope.ServiceProvider.GetRequiredService<IExpertStatisticsService>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SettlePositionsJob>>();
        
        var job = new SettlePositionsJob(dbContext, expertStatisticsService, mediator, logger);
        await job.SettlePositions();
    }
}


using Hangfire.Dashboard;

namespace Rebet.Infrastructure.BackgroundJobs;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In production, implement proper authorization
        // For now, allow access in development
        return true;
    }
}


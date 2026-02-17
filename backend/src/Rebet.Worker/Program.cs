using Rebet.Application.Commands.Auth;
using Rebet.Application.Interfaces;
using Rebet.Infrastructure.BackgroundJobs;
using Rebet.Infrastructure.Persistence;
using Rebet.Infrastructure.Repositories;
using Rebet.Infrastructure.Services;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Rebet.Worker")
    .CreateLogger());

var postgresConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

builder.Services.AddHealthChecks()
    .AddNpgSql(postgresConnection)
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), new[] { "live" });

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(postgresConnection));

// Hangfire storage + server (Worker processes jobs)
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(opt => opt.UseNpgsqlConnection(postgresConnection), new PostgreSqlStorageOptions
    {
        SchemaName = "hangfire"
    }));
builder.Services.AddHangfireServer();

// MediatR + FluentValidation (required by job handlers)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(RegisterCommandValidator).Assembly);
builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Rebet.Application.Behaviors.ValidationBehavior<,>));

// Caching
builder.Services.AddMemoryCache();
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "Rebet:";
    });
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(redisConnectionString));
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddHttpClient<IOddsProviderService, OddsProviderService>();

builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IExpertStatisticsService, ExpertStatisticsService>();

const string redisInstancePrefix = "Rebet:";
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddScoped<ICacheService>(sp =>
    {
        var memoryCache = sp.GetRequiredService<IMemoryCache>();
        var distributedCache = sp.GetRequiredService<IDistributedCache>();
        var redis = sp.GetService<IConnectionMultiplexer>();
        return redis != null
            ? new CacheService(memoryCache, distributedCache, redis, redisInstancePrefix)
            : new CacheService(memoryCache, distributedCache, redisInstancePrefix);
    });
}
else
{
    builder.Services.AddScoped<ICacheService>(sp =>
    {
        var memoryCache = sp.GetRequiredService<IMemoryCache>();
        var distributedCache = sp.GetRequiredService<IDistributedCache>();
        return new CacheService(memoryCache, distributedCache);
    });
}

builder.Services.AddScoped<SyncEventsJob>();
builder.Services.AddScoped<SettlePositionsJob>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IExpertRepository, ExpertRepository>();
builder.Services.AddScoped<ISportEventRepository, SportEventRepository>();
builder.Services.AddScoped<IPositionRepository, PositionRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IVoteRepository, VoteRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<INewsfeedRepository, NewsfeedRepository>();
builder.Services.AddScoped<ITicketFollowRepository, TicketFollowRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

SyncEventsJobWrapper.SetServiceProvider(app.Services);
SettlePositionsJobWrapper.SetServiceProvider(app.Services);

RecurringJob.AddOrUpdate(
    "sync-events-job",
    () => SyncEventsJobWrapper.ExecuteSyncHotEvents(),
    "*/1 * * * *",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

RecurringJob.AddOrUpdate(
    "settle-positions-job",
    () => SettlePositionsJobWrapper.ExecuteSettlePositions(),
    "*/5 * * * *",
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

app.Logger.LogInformation("Rebet.Worker started. Hangfire dashboard at /hangfire");
app.Run();

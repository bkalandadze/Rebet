using Rebet.Application.Commands.Auth;
using Rebet.Application.Interfaces;
using Rebet.Infrastructure.BackgroundJobs;
using Rebet.Infrastructure.Persistence;
using Rebet.Infrastructure.Repositories;
using Rebet.Infrastructure.Services;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Npgsql;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.Text;
using System.Threading.RateLimiting;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .Enrich.FromLogContext()
    .Enrich.WithThreadId()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for logging
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddVersionedApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

// Swagger with versioning - configure after AddVersionedApiExplorer
builder.Services.AddSwaggerGen();

// CORS Configuration
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() 
    ?? new[] { "http://localhost:3000", "http://localhost:5173", "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("authenticated", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? throw new InvalidOperationException("User not authenticated"),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("strict", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", token);
    };
});

// Connection strings - use DefaultConnection (config or env, e.g. Docker)
var postgresConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("PostgreSQL connection string is not configured (ConnectionStrings:DefaultConnection).");

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? "localhost:6379";


var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddNpgSql(postgresConnection)
    .AddRedis(redisConnectionString)
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), new[] { "live" });



// JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };
});

builder.Services.AddAuthorization();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(postgresConnection));

// Database Seeder
builder.Services.AddScoped<Rebet.Infrastructure.Persistence.DatabaseSeeder>();

// Hangfire - use same connection string
var hangfireConnectionString = postgresConnection;
// Hangfire storage only (no server - Worker processes jobs)
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(opt => opt.UseNpgsqlConnection(hangfireConnectionString), new PostgreSqlStorageOptions
    {
        SchemaName = "hangfire"
    }));

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(RegisterCommandValidator).Assembly);
builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(Rebet.Application.Behaviors.ValidationBehavior<,>));

// Caching
builder.Services.AddMemoryCache();

// Redis Cache Configuration (reuse variable from health checks)
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "Rebet:";
    });

    // Register IConnectionMultiplexer for pattern-based cache invalidation
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(redisConnectionString));
}
else
{
    // Fallback to in-memory distributed cache if Redis is not configured
    builder.Services.AddDistributedMemoryCache();
}

// HTTP Clients
builder.Services.AddHttpClient<IOddsProviderService, OddsProviderService>();

// Application Services
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IExpertStatisticsService, ExpertStatisticsService>();

// Cache Service - register with IConnectionMultiplexer if available
const string redisInstancePrefix = "Rebet:";
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddScoped<ICacheService>(sp =>
    {
        var memoryCache = sp.GetRequiredService<IMemoryCache>();
        var distributedCache = sp.GetRequiredService<IDistributedCache>();
        var redisConnection = sp.GetService<IConnectionMultiplexer>();
        return redisConnection != null
            ? new CacheService(memoryCache, distributedCache, redisConnection, redisInstancePrefix)
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

// Background Jobs
builder.Services.AddScoped<SyncEventsJob>();
builder.Services.AddScoped<SettlePositionsJob>();

// Repositories
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

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// SignalR with Redis backplane
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    })
    .AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("Rebet:SignalR");
    });
}
else
{
    // Fallback to in-memory if Redis is not configured
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    });
}

var app = builder.Build();

// Run migrations on startup when enabled (e.g. Docker; idempotent if already applied)
if (app.Configuration.GetValue<bool>("RunMigrationsOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    const int maxRetries = 10;
    const int delayMs = 2000;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await dbContext.Database.MigrateAsync();
            app.Logger.LogInformation("Database migrations applied successfully");
            break;
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "57P03" && attempt < maxRetries)
        {
            app.Logger.LogWarning("Database is still starting up (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms...",
                attempt, maxRetries, delayMs);
            await Task.Delay(delayMs);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Error applying database migrations");
            break;
        }
    }
}

// Seed database on startup (Development only, and only if enabled)
if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Database:SeedOnStartup", false))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<Rebet.Infrastructure.Persistence.DatabaseSeeder>();
    
    // Retry logic for database startup (PostgreSQL might still be initializing)
    const int maxRetries = 10;
    const int delayMs = 2000; // 2 seconds
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            await seeder.SeedAsync();
            app.Logger.LogInformation("Database seeding completed");
            break; // Success, exit retry loop
        }
        catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "57P03" && attempt < maxRetries)
        {
            // Database is still starting up, retry
            app.Logger.LogWarning("Database is still starting up (attempt {Attempt}/{MaxRetries}), retrying in {Delay}ms...", 
                attempt, maxRetries, delayMs);
            await Task.Delay(delayMs);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Error seeding database");
            // Don't throw - allow app to start even if seeding fails
            break;
        }
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var apiVersionDescriptionProvider = app.Services
            .GetRequiredService<IApiVersionDescriptionProvider>();

        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                $"Betting Platform API {description.GroupName.ToUpperInvariant()}");
        }
    });
}

// Only redirect to HTTPS when the app is configured to listen on HTTPS (avoid "Failed to determine the https port" in Docker)
var urls = app.Configuration["ASPNETCORE_URLS"] ?? "";
if (urls.Contains("https", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

// CORS (must be before UseAuthentication)
app.UseCors();

// Rate Limiting
app.UseRateLimiter();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

app.MapControllers();

// Map SignalR Hub
app.MapHub<Rebet.Infrastructure.Hubs.NewsfeedHub>("/hubs/newsfeed");

app.Run();

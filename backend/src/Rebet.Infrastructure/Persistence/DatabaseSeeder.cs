using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Rebet.Infrastructure.Persistence;

public class DatabaseSeeder
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly ApplicationDbContext _dbContext;

    public DatabaseSeeder(
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger,
        ApplicationDbContext dbContext)
    {
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Check if data already exists
        var userCount = await _dbContext.Users.CountAsync(u => !u.IsDeleted, cancellationToken);
        
        if (userCount > 0)
        {
            _logger.LogInformation("Database already contains {UserCount} users. Skipping seed.", userCount);
            return;
        }

        _logger.LogInformation("Starting database seeding...");

        // Get connection string (same logic as Program.cs - uses Aspire's injected connection string)
        var connectionString = _configuration.GetConnectionString("postgres") 
            ?? _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("PostgreSQL connection string is not configured");

        // Fix connection string if Aspire generated it with default "postgres" user
        if (connectionString.Contains("Username=postgres") || connectionString.Contains("User Id=postgres"))
        {
            connectionString = connectionString
                .Replace("Username=postgres", "Username=admin")
                .Replace("User Id=postgres", "User Id=admin");
            
            if (System.Text.RegularExpressions.Regex.IsMatch(connectionString, @"Password=[^;]+"))
            {
                connectionString = System.Text.RegularExpressions.Regex.Replace(connectionString, @"Password=[^;]+", "Password=admin");
            }
            else
            {
                connectionString += ";Password=admin";
            }
        }

        // Get the SQL script path
        var scriptPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "scripts",
            "seed-dummy-data.sql"
        );

        // Normalize the path
        scriptPath = Path.GetFullPath(scriptPath);

        if (!File.Exists(scriptPath))
        {
            _logger.LogWarning("Seed script not found at {ScriptPath}. Skipping seed.", scriptPath);
            return;
        }

        _logger.LogInformation("Reading seed script from {ScriptPath}", scriptPath);

        try
        {
            var sqlScript = await File.ReadAllTextAsync(scriptPath, cancellationToken);
            
            _logger.LogInformation("Executing seed script...");
            var startTime = DateTime.UtcNow;

            // Execute SQL script using Npgsql directly
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Execute the entire script at once
            // Npgsql can handle multiple statements separated by semicolons
            await using var command = new NpgsqlCommand(sqlScript, connection);
            command.CommandTimeout = 300; // 5 minutes timeout for large scripts
            
            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "42P07" || pgEx.SqlState == "42710")
            {
                // Ignore "relation already exists" and "duplicate object" errors
                // These are common in seed scripts with IF NOT EXISTS logic
                _logger.LogInformation("Some objects may already exist (this is normal): {Message}", pgEx.Message);
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Seed script executed in {Duration}ms", duration.TotalMilliseconds);

            // Verify seeding
            var newUserCount = await _dbContext.Users.CountAsync(u => !u.IsDeleted, cancellationToken);
            _logger.LogInformation("Database seeding completed. Current user count: {UserCount}", newUserCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing seed script");
            throw;
        }
    }
}


using Rebet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Rebet.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserProfile> UserProfiles { get; set; } = null!;
    public DbSet<Wallet> Wallets { get; set; } = null!;
    public DbSet<Expert> Experts { get; set; } = null!;
    public DbSet<ExpertStatistics> ExpertStatistics { get; set; } = null!;
    public DbSet<SportEvent> SportEvents { get; set; } = null!;
    public DbSet<EventResult> EventResults { get; set; } = null!;
    public DbSet<Position> Positions { get; set; } = null!;
    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<TicketEntry> TicketEntries { get; set; } = null!;
    public DbSet<Vote> Votes { get; set; } = null!;
    public DbSet<Subscription> Subscriptions { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
    public DbSet<NewsfeedItem> NewsfeedItems { get; set; } = null!;
    public DbSet<TicketFollow> TicketFollows { get; set; } = null!;
    public DbSet<EventMarketData> EventMarketData { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}


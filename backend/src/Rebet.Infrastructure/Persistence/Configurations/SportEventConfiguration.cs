using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class SportEventConfiguration : IEntityTypeConfiguration<SportEvent>
{
    public void Configure(EntityTypeBuilder<SportEvent> builder)
    {
        builder.ToTable("sport_events");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.ExternalEventId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.HasIndex(e => e.ExternalEventId)
            .IsUnique();
        
        builder.Property(e => e.Sport)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(e => e.League)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(e => e.HomeTeam)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(e => e.AwayTeam)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(e => e.HomeTeamLogo)
            .HasMaxLength(500);
        
        builder.Property(e => e.AwayTeamLogo)
            .HasMaxLength(500);
        
        builder.Property(e => e.StartTimeUtc)
            .IsRequired();
        
        builder.Property(e => e.StartTimeEpoch)
            .IsRequired();
        
        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(EventStatus.Scheduled);
        
        builder.Property(e => e.HomeWinOdds)
            .HasPrecision(10, 2);
        
        builder.Property(e => e.DrawOdds)
            .HasPrecision(10, 2);
        
        builder.Property(e => e.AwayWinOdds)
            .HasPrecision(10, 2);
        
        builder.Property(e => e.Over25Odds)
            .HasPrecision(10, 2);
        
        builder.Property(e => e.Under25Odds)
            .HasPrecision(10, 2);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
        
        // Indexes
        builder.HasIndex(e => new { e.Status, e.StartTimeEpoch })
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(e => new { e.Sport, e.Status })
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(e => e.StartTimeEpoch)
            .HasFilter("\"IsDeleted\" = false");
        
        // Relationships
        builder.HasMany(e => e.Positions)
            .WithOne(p => p.SportEvent)
            .HasForeignKey(p => p.SportEventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


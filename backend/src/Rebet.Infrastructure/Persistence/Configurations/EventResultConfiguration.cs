using Rebet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class EventResultConfiguration : IEntityTypeConfiguration<EventResult>
{
    public void Configure(EntityTypeBuilder<EventResult> builder)
    {
        builder.ToTable("event_results");
        
        builder.HasKey(er => er.Id);
        
        builder.Property(er => er.SportEventId)
            .IsRequired();
        
        builder.Property(er => er.FinalScore)
            .HasMaxLength(20);
        
        builder.Property(er => er.Winner)
            .HasMaxLength(10);
        
        builder.Property(er => er.HalfTimeScore)
            .HasMaxLength(20);
        
        // JSONB column
        builder.Property(er => er.MarketResultsJson)
            .HasColumnType("jsonb");
        
        builder.Property(er => er.CompletedAt)
            .IsRequired();
        
        builder.Property(er => er.SettledAt)
            .IsRequired();
        
        // Unique constraint: one result per sport event
        builder.HasIndex(er => er.SportEventId)
            .IsUnique();
        
        // Indexes
        builder.HasIndex(er => er.SettledAt)
            .IsDescending();
        
        // Relationships
        builder.HasOne(er => er.SportEvent)
            .WithOne(se => se.Result)
            .HasForeignKey<EventResult>(er => er.SportEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


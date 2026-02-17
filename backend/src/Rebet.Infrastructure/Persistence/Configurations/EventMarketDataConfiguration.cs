using Rebet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class EventMarketDataConfiguration : IEntityTypeConfiguration<EventMarketData>
{
    public void Configure(EntityTypeBuilder<EventMarketData> builder)
    {
        builder.ToTable("event_market_data");
        
        builder.HasKey(emd => emd.Id);
        
        builder.Property(emd => emd.SportEventId)
            .IsRequired();
        
        // JSONB column with GIN index
        builder.Property(emd => emd.MarketsJson)
            .IsRequired()
            .HasColumnType("jsonb");
        
        builder.Property(emd => emd.SnapshotEpoch)
            .IsRequired();
        
        builder.Property(emd => emd.SnapshotAt)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(emd => emd.SportEventId);
        
        // GIN index for JSONB column
        builder.HasIndex(emd => emd.MarketsJson)
            .HasMethod("gin");
        
        // Relationships
        builder.HasOne(emd => emd.SportEvent)
            .WithOne(se => se.MarketData)
            .HasForeignKey<EventMarketData>(emd => emd.SportEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


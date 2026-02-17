using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("positions", t =>
            t.HasCheckConstraint("CK_Position_Odds_Positive", "\"Odds\" > 0"));
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.CreatorId)
            .IsRequired();
        
        builder.Property(p => p.CreatorType)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(p => p.SportEventId)
            .IsRequired();
        
        builder.Property(p => p.Market)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(p => p.Selection)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(p => p.Odds)
            .IsRequired()
            .HasPrecision(10, 2);
        
        builder.Property(p => p.Analysis)
            .HasMaxLength(5000);
        
        builder.Property(p => p.Status)
            .HasConversion<int>()
            .HasDefaultValue(PositionStatus.Pending);
        
        builder.Property(p => p.Result)
            .HasConversion<int?>()
            .IsRequired(false);
        
        builder.Property(p => p.ViewCount)
            .HasDefaultValue(0);
        
        builder.Property(p => p.UpvoteCount)
            .HasDefaultValue(0);
        
        builder.Property(p => p.DownvoteCount)
            .HasDefaultValue(0);
        
        builder.Property(p => p.VoterCount)
            .HasDefaultValue(0);
        
        builder.Property(p => p.PredictionPercentage)
            .HasPrecision(5, 2)
            .HasDefaultValue(0.00m);
        
        builder.HasQueryFilter(p => !p.IsDeleted);
        
        // Indexes
        builder.HasIndex(p => p.CreatorId)
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(p => p.SportEventId)
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(p => p.Status)
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(p => new { p.CreatorType, p.Status })
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(p => p.UpvoteCount)
            .IsDescending()
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(p => p.CreatedAt)
            .IsDescending()
            .HasFilter("\"IsDeleted\" = false");
        
        // Relationships
        builder.HasOne(p => p.Creator)
            .WithMany(u => u.Positions)
            .HasForeignKey(p => p.CreatorId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(p => p.SportEvent)
            .WithMany(e => e.Positions)
            .HasForeignKey(p => p.SportEventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


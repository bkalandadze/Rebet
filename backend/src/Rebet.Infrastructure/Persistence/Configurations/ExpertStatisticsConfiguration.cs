using Rebet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class ExpertStatisticsConfiguration : IEntityTypeConfiguration<ExpertStatistics>
{
    public void Configure(EntityTypeBuilder<ExpertStatistics> builder)
    {
        builder.ToTable("expert_statistics", t =>
        {
            t.HasCheckConstraint("CK_ExpertStatistics_WinRate_Range", "\"WinRate\" >= 0 AND \"WinRate\" <= 100");
            t.HasCheckConstraint("CK_ExpertStatistics_Last7DaysWinRate_Range", "\"Last7DaysWinRate\" >= 0 AND \"Last7DaysWinRate\" <= 100");
            t.HasCheckConstraint("CK_ExpertStatistics_Last30DaysWinRate_Range", "\"Last30DaysWinRate\" >= 0 AND \"Last30DaysWinRate\" <= 100");
            t.HasCheckConstraint("CK_ExpertStatistics_Last90DaysWinRate_Range", "\"Last90DaysWinRate\" >= 0 AND \"Last90DaysWinRate\" <= 100");
        });
        
        builder.HasKey(s => s.ExpertId);
        
        builder.Property(s => s.WinRate)
            .HasPrecision(5, 2);
        
        builder.Property(s => s.ROI)
            .HasPrecision(5, 2);
        
        builder.Property(s => s.AverageOdds)
            .HasPrecision(10, 2);
        
        builder.Property(s => s.Last7DaysWinRate)
            .HasPrecision(5, 2);
        
        builder.Property(s => s.Last30DaysWinRate)
            .HasPrecision(5, 2);
        
        builder.Property(s => s.Last90DaysWinRate)
            .HasPrecision(5, 2);
        
        builder.Property(s => s.TotalProfit)
            .HasPrecision(18, 2);
        
        builder.Property(s => s.TotalCommissionEarned)
            .HasPrecision(18, 2);
        
        // Indexes
        builder.HasIndex(s => s.WinRate)
            .IsDescending();
        
        builder.HasIndex(s => s.ROI)
            .IsDescending();
        
        // Relationship
        builder.HasOne(s => s.Expert)
            .WithOne(e => e.Statistics)
            .HasForeignKey<ExpertStatistics>(s => s.ExpertId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


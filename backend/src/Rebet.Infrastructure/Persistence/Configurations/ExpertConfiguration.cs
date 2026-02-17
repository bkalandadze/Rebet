using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class ExpertConfiguration : IEntityTypeConfiguration<Expert>
{
    public void Configure(EntityTypeBuilder<Expert> builder)
    {
        builder.ToTable("experts");
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.UserId)
            .IsRequired();
        
        builder.Property(e => e.DisplayName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(e => e.Bio)
            .HasMaxLength(5000);
        
        builder.Property(e => e.Specialization)
            .HasMaxLength(100);
        
        builder.Property(e => e.Tier)
            .HasConversion<int>()
            .HasDefaultValue(ExpertTier.Bronze);
        
        builder.Property(e => e.Status)
            .HasConversion<int>()
            .HasDefaultValue(ExpertStatus.PendingApproval);
        
        builder.Property(e => e.CommissionRate)
            .HasPrecision(5, 4)
            .HasDefaultValue(0.1000m);
        
        builder.Property(e => e.IsVerified)
            .HasDefaultValue(false);
        
        builder.Property(e => e.UpvoteCount)
            .HasDefaultValue(0);
        
        builder.Property(e => e.DownvoteCount)
            .HasDefaultValue(0);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
        
        // Unique constraint: one expert per user (where not deleted)
        builder.HasIndex(e => e.UserId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        
        // Indexes
        builder.HasIndex(e => e.Status)
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(e => e.Specialization)
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(e => e.UpvoteCount)
            .IsDescending()
            .HasFilter("\"IsDeleted\" = false");
        
        // Relationships
        builder.HasOne(e => e.User)
            .WithOne(u => u.Expert)
            .HasForeignKey<Expert>(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.Statistics)
            .WithOne(s => s.Expert)
            .HasForeignKey<ExpertStatistics>(s => s.ExpertId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


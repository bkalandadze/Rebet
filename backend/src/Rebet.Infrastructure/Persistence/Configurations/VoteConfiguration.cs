using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.ToTable("votes");
        
        builder.HasKey(v => v.Id);
        
        builder.Property(v => v.UserId)
            .IsRequired();
        
        builder.Property(v => v.VoteableType)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(v => v.VoteableId)
            .IsRequired();
        
        builder.Property(v => v.Type)
            .HasConversion<int>()
            .IsRequired();
        
        // Unique constraint: one vote per user per voteable item
        builder.HasIndex(v => new { v.UserId, v.VoteableType, v.VoteableId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasQueryFilter(v => !v.IsDeleted);
        
        // Indexes
        builder.HasIndex(v => new { v.VoteableType, v.VoteableId });
        
        builder.HasIndex(v => v.UserId);
        
        // Relationships
        builder.HasOne(v => v.User)
            .WithMany(u => u.Votes)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


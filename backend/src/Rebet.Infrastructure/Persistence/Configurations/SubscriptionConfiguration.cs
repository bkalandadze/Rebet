using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");
        
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.UserId)
            .IsRequired();
        
        builder.Property(s => s.ExpertId)
            .IsRequired();
        
        builder.Property(s => s.Status)
            .HasConversion<int>()
            .HasDefaultValue(SubscriptionStatus.Active);
        
        builder.Property(s => s.ReceiveNotifications)
            .HasDefaultValue(true);
        
        builder.Property(s => s.SubscribedAt)
            .HasDefaultValueSql("NOW()");
        
        // Unique constraint: one subscription per user-expert pair
        builder.HasIndex(s => new { s.UserId, s.ExpertId })
            .IsUnique();
        
        // Indexes
        builder.HasIndex(s => s.UserId);
        
        builder.HasIndex(s => s.ExpertId);
        
        builder.HasIndex(s => s.Status);
        
        // Relationships
        builder.HasOne(s => s.User)
            .WithMany(u => u.Subscriptions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(s => s.Expert)
            .WithMany(e => e.Subscribers)
            .HasForeignKey(s => s.ExpertId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


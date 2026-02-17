using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        
        builder.HasKey(n => n.Id);
        
        builder.Property(n => n.UserId)
            .IsRequired();
        
        builder.Property(n => n.Type)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(2000);
        
        builder.Property(n => n.ActionUrl)
            .HasMaxLength(500);
        
        builder.Property(n => n.IsRead)
            .HasDefaultValue(false);
        
        // JSONB column
        builder.Property(n => n.MetadataJson)
            .HasColumnType("jsonb");
        
        // Indexes
        builder.HasIndex(n => n.UserId);
        
        builder.HasIndex(n => new { n.UserId, n.IsRead })
            .HasFilter("\"IsRead\" = false");
        
        builder.HasIndex(n => n.CreatedAt)
            .IsDescending();
        
        // Relationships
        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


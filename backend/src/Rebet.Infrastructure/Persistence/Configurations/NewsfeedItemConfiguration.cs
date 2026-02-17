using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class NewsfeedItemConfiguration : IEntityTypeConfiguration<NewsfeedItem>
{
    public void Configure(EntityTypeBuilder<NewsfeedItem> builder)
    {
        builder.ToTable("newsfeed_items");
        
        builder.HasKey(n => n.Id);
        
        builder.Property(n => n.Type)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(n => n.Description)
            .HasMaxLength(2000);
        
        builder.Property(n => n.ActionUrl)
            .HasMaxLength(500);
        
        builder.Property(n => n.ExpertId)
            .IsRequired(false);
        
        builder.Property(n => n.PositionId)
            .IsRequired(false);
        
        builder.Property(n => n.TicketId)
            .IsRequired(false);
        
        // JSONB column
        builder.Property(n => n.MetadataJson)
            .HasColumnType("jsonb");
        
        builder.HasQueryFilter(n => !n.IsDeleted);
        
        // Indexes
        builder.HasIndex(n => n.CreatedAt)
            .IsDescending()
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(n => n.Type)
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(n => n.ExpertId)
            .HasFilter("\"IsDeleted\" = false");
        
        // Relationships
        builder.HasOne(n => n.Expert)
            .WithMany()
            .HasForeignKey(n => n.ExpertId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasOne(n => n.Position)
            .WithMany()
            .HasForeignKey(n => n.PositionId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasOne(n => n.Ticket)
            .WithMany()
            .HasForeignKey(n => n.TicketId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}


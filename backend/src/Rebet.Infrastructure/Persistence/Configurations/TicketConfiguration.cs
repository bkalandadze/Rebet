using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets", t =>
        {
            t.HasCheckConstraint("CK_Ticket_Stake_Positive", "\"Stake\" > 0");
            t.HasCheckConstraint("CK_Ticket_Odds_Positive", "\"TotalOdds\" > 0");
            t.HasCheckConstraint("CK_Ticket_PotentialReturn_Valid", "\"PotentialReturn\" >= \"Stake\"");
        });
        
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.ExpertId)
            .IsRequired();
        
        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(t => t.Description)
            .HasMaxLength(5000);
        
        builder.Property(t => t.Type)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(t => t.Status)
            .HasConversion<int>()
            .HasDefaultValue(TicketStatus.Draft);
        
        builder.Property(t => t.TotalOdds)
            .IsRequired()
            .HasPrecision(10, 2);
        
        builder.Property(t => t.Stake)
            .IsRequired()
            .HasPrecision(10, 2);
        
        builder.Property(t => t.PotentialReturn)
            .IsRequired()
            .HasPrecision(15, 2);
        
        builder.Property(t => t.Visibility)
            .HasConversion<int>()
            .HasDefaultValue(TicketVisibility.Public);
        
        builder.Property(t => t.Result)
            .HasConversion<int?>()
            .IsRequired(false);
        
        builder.Property(t => t.FinalOdds)
            .HasPrecision(10, 2);
        
        builder.Property(t => t.SettlementNotes)
            .HasMaxLength(2000);
        
        builder.Property(t => t.ViewCount)
            .HasDefaultValue(0);
        
        builder.Property(t => t.FollowerCount)
            .HasDefaultValue(0);
        
        builder.Property(t => t.UpvoteCount)
            .HasDefaultValue(0);
        
        builder.Property(t => t.DownvoteCount)
            .HasDefaultValue(0);
        
        builder.Property(t => t.CommentCount)
            .HasDefaultValue(0);
        
        builder.HasQueryFilter(t => !t.IsDeleted);
        
        // Indexes
        builder.HasIndex(t => t.ExpertId)
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(t => t.Status)
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(t => t.CreatedAt)
            .IsDescending()
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(t => t.UpvoteCount)
            .IsDescending()
            .HasFilter("\"IsDeleted\" = false");
        
        // Relationships
        // Note: ExpertId is a foreign key to Users table, but no navigation property exists
        // The relationship is handled through queries when needed
        
        builder.HasMany(t => t.Entries)
            .WithOne(te => te.Ticket)
            .HasForeignKey(te => te.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


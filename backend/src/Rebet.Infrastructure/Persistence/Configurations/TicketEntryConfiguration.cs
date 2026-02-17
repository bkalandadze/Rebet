using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class TicketEntryConfiguration : IEntityTypeConfiguration<TicketEntry>
{
    public void Configure(EntityTypeBuilder<TicketEntry> builder)
    {
        builder.ToTable("ticket_entries", t =>
            t.HasCheckConstraint("CK_TicketEntry_Odds_Positive", "\"Odds\" > 0"));
        
        builder.HasKey(te => te.Id);
        
        builder.Property(te => te.TicketId)
            .IsRequired();
        
        builder.Property(te => te.SportEventId)
            .IsRequired();
        
        builder.Property(te => te.Sport)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(te => te.League)
            .HasMaxLength(100);
        
        builder.Property(te => te.HomeTeam)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(te => te.AwayTeam)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(te => te.EventStartTime)
            .IsRequired();
        
        builder.Property(te => te.Market)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(te => te.Selection)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(te => te.Odds)
            .IsRequired()
            .HasPrecision(10, 2);
        
        builder.Property(te => te.Handicap)
            .HasMaxLength(20);
        
        builder.Property(te => te.Status)
            .HasConversion<int>()
            .HasDefaultValue(EntryStatus.Pending);
        
        builder.Property(te => te.Result)
            .HasConversion<int?>()
            .IsRequired(false);
        
        builder.Property(te => te.ResultNotes)
            .HasMaxLength(1000);
        
        builder.Property(te => te.Analysis)
            .HasMaxLength(2000);
        
        builder.Property(te => te.DisplayOrder)
            .HasDefaultValue(0);
        
        // Indexes
        builder.HasIndex(te => te.TicketId);
        
        builder.HasIndex(te => te.SportEventId);
        
        builder.HasIndex(te => te.Status);
        
        // Relationships
        builder.HasOne(te => te.Ticket)
            .WithMany(t => t.Entries)
            .HasForeignKey(te => te.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(te => te.SportEvent)
            .WithMany(se => se.TicketEntries)
            .HasForeignKey(te => te.SportEventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


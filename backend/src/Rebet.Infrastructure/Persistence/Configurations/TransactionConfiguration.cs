using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions", t =>
        {
            t.HasCheckConstraint("CK_Transaction_Amount_Positive", "\"Amount\" > 0");
            t.HasCheckConstraint("CK_Transaction_Balance_Consistent",
                "\"BalanceAfter\" = \"BalanceBefore\" + \"Amount\" * CASE WHEN \"Type\" IN (1, 3, 4) THEN 1 WHEN \"Type\" = 2 THEN -1 ELSE 0 END");
        });
        
        builder.HasKey(t => t.Id);
        
        builder.Property(t => t.WalletId)
            .IsRequired();
        
        builder.Property(t => t.TransactionReference)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(t => t.TransactionReference)
            .IsUnique();
        
        builder.Property(t => t.Type)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(t => t.Amount)
            .IsRequired()
            .HasPrecision(15, 2);
        
        builder.Property(t => t.BalanceBefore)
            .IsRequired()
            .HasPrecision(15, 2);
        
        builder.Property(t => t.BalanceAfter)
            .IsRequired()
            .HasPrecision(15, 2);
        
        builder.Property(t => t.Status)
            .HasConversion<int>()
            .HasDefaultValue(TransactionStatus.Pending);
        
        builder.Property(t => t.Description)
            .HasMaxLength(1000);
        
        // JSONB column
        builder.Property(t => t.MetadataJson)
            .HasColumnType("jsonb");
        
        builder.Property(t => t.PaymentMethod)
            .HasMaxLength(50);
        
        builder.Property(t => t.ExternalTransactionId)
            .HasMaxLength(100);
        
        // Indexes
        builder.HasIndex(t => t.WalletId);
        
        builder.HasIndex(t => t.Type);
        
        builder.HasIndex(t => t.Status);
        
        builder.HasIndex(t => t.CreatedAt)
            .IsDescending();
        
        // Relationships
        builder.HasOne(t => t.Wallet)
            .WithMany()
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(t => t.RelatedExpert)
            .WithMany()
            .HasForeignKey(t => t.RelatedExpertId)
            .OnDelete(DeleteBehavior.SetNull);
        
        builder.HasOne(t => t.RelatedTicket)
            .WithMany()
            .HasForeignKey(t => t.RelatedTicketId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}


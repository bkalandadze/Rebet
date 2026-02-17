using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets", t =>
        {
            t.HasCheckConstraint("CK_Wallet_Balance_NonNegative", "\"Balance\" >= 0");
            t.HasCheckConstraint("CK_Wallet_PendingBalance_NonNegative", "\"PendingBalance\" >= 0");
        });
        
        builder.HasKey(w => w.Id);
        
        builder.Property(w => w.Balance)
            .HasPrecision(18, 2)
            .HasDefaultValue(0.00m);
        
        builder.Property(w => w.PendingBalance)
            .HasPrecision(18, 2)
            .HasDefaultValue(0.00m);
        
        builder.Property(w => w.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("USD");
        
        builder.Property(w => w.Status)
            .HasConversion<int>()
            .HasDefaultValue(WalletStatus.Active);
        
        builder.Property(w => w.LastUpdatedAt)
            .HasDefaultValueSql("NOW()");
        
        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}


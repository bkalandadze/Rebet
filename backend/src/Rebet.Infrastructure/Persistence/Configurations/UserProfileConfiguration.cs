using Rebet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");
        
        builder.HasKey(p => p.UserId);
        
        builder.Property(p => p.DisplayName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(p => p.DisplayName);
        
        builder.Property(p => p.Avatar)
            .HasMaxLength(500);
        
        builder.Property(p => p.TimeZone)
            .HasMaxLength(50);
        
        builder.Property(p => p.PreferredLanguage)
            .HasMaxLength(10)
            .HasDefaultValue("en");
        
        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("NOW()");
        
        builder.Property(p => p.UpdatedAt)
            .HasDefaultValueSql("NOW()");
    }
}


using Rebet.Domain.Entities;
using Rebet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rebet.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.UserId)
            .IsRequired();
        
        builder.Property(c => c.CommentableType)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(c => c.CommentableId)
            .IsRequired();
        
        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(5000);
        
        builder.Property(c => c.ParentCommentId)
            .IsRequired(false);
        
        builder.Property(c => c.UpdatedAt)
            .HasDefaultValueSql("NOW()");
        
        builder.HasQueryFilter(c => !c.IsDeleted);
        
        // Indexes
        builder.HasIndex(c => new { c.CommentableType, c.CommentableId })
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(c => c.UserId)
            .HasFilter("\"IsDeleted\" = false");
        
        builder.HasIndex(c => c.CreatedAt)
            .IsDescending()
            .HasFilter("\"IsDeleted\" = false");
        
        // Relationships
        builder.HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(c => c.ParentComment)
            .WithMany(p => p.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


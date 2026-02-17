using Rebet.Domain.Enums;

namespace Rebet.Domain.Entities;

public class Comment : BaseEntity
{
    public Guid UserId { get; set; }
    public CommentableType CommentableType { get; set; }
    public Guid CommentableId { get; set; }
    
    public string Content { get; set; } = null!;
    public Guid? ParentCommentId { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public User User { get; set; } = null!;
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}


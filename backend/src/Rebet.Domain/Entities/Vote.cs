using Rebet.Domain.Enums;

namespace Rebet.Domain.Entities;

public class Vote : BaseEntity
{
    public Guid UserId { get; set; }
    public VoteableType VoteableType { get; set; }
    public Guid VoteableId { get; set; }
    public VoteType Type { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
}


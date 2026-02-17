using Rebet.Domain.Enums;

namespace Rebet.Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid WalletId { get; set; }
    
    public string TransactionReference { get; set; } = null!;
    
    public TransactionType Type { get; set; }
    
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    
    public string? Description { get; set; }
    
    public string? MetadataJson { get; set; } // JSONB
    
    public Guid? RelatedExpertId { get; set; }
    public Guid? RelatedTicketId { get; set; }
    
    public string? PaymentMethod { get; set; }
    public string? ExternalTransactionId { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    // Navigation
    public Wallet Wallet { get; set; } = null!;
    public Expert? RelatedExpert { get; set; }
    public Ticket? RelatedTicket { get; set; }
}


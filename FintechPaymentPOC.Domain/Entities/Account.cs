namespace FintechPaymentPOC.Domain.Entities;

public class Account : Entity
{
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();
    public virtual ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();
}


namespace FintechPaymentPOC.Domain.Events;

public class PaymentRequestedEvent : IDomainEvent
{
    public Guid TransactionId { get; set; }
    public Guid SenderAccountId { get; set; }
    public Guid ReceiverAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    public string SenderName { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    
    // Keep Timestamp for backward compatibility
    public DateTime Timestamp => OccurredOn;
}


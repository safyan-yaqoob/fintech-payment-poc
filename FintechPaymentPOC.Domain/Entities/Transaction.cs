using FintechPaymentPOC.Domain.Events;

namespace FintechPaymentPOC.Domain.Entities;

public class Transaction : Entity
{
    public Guid SenderAccountId { get; set; }
    public Guid ReceiverAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? FailureReason { get; set; }
    
    /// <summary>
    /// ISO 20022 pacs.008 XML representation of this payment
    /// Generated during payment creation using MT103 → pacs.008 conversion pipeline
    /// </summary>
    public string? Pacs008Xml { get; set; }

    // Navigation properties
    public virtual Account? SenderAccount { get; set; }
    public virtual Account? ReceiverAccount { get; set; }

    /// <summary>
    /// Raises a PaymentRequestedEvent when a payment is requested
    /// </summary>
    public void RequestPayment(string senderName, string receiverName)
    {
        var paymentEvent = new PaymentRequestedEvent
        {
            TransactionId = Id,
            SenderAccountId = SenderAccountId,
            ReceiverAccountId = ReceiverAccountId,
            Amount = Amount,
            Currency = Currency,
            OccurredOn = DateTime.UtcNow,
            SenderName = senderName,
            ReceiverName = receiverName
        };

        AddDomainEvent(paymentEvent);
    }
}

public enum TransactionStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Processing = 3
}


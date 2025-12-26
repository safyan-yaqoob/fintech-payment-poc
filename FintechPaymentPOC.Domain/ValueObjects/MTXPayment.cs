namespace FintechPaymentPOC.Domain.ValueObjects;

public class MTXPayment
{
    public Guid TransactionId { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Receiver { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public decimal? FraudScore { get; set; }
}


namespace FintechPaymentPOC.Application.DTOs;

public class TransactionDto
{
    public Guid Id { get; set; }
    public Guid SenderAccountId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public Guid ReceiverAccountId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? FailureReason { get; set; }
}


namespace FintechPaymentPOC.Application.DTOs;

public class PaymentRequestDto
{
    public Guid SenderAccountId { get; set; }
    public Guid ReceiverAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
}


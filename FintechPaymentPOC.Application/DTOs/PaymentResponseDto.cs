using FintechPaymentPOC.Domain.ValueObjects;

namespace FintechPaymentPOC.Application.DTOs;

public class PaymentResponseDto
{
    public Guid TransactionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public MTXPayment? MTXPayment { get; set; }
    public string? Message { get; set; }
    
    /// <summary>
    /// ISO 20022 pacs.008 XML representation of the payment
    /// Generated using MT103 → pacs.008 conversion pipeline
    /// </summary>
    public string? Pacs008Xml { get; set; }
}


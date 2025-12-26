using FintechPaymentPOC.Application.DTOs;
using FintechPaymentPOC.Application.Interfaces;
using FintechPaymentPOC.Domain.Entities;
using FintechPaymentPOC.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FintechPaymentPOC.Application.Services;

/// <summary>
/// Converts payment data to SwiftMtxPayment format
/// Enables integration of MT103 → pacs.008 conversion into regular payment flow
/// </summary>
public class PaymentToSwiftConverter : IPaymentToSwiftConverter
{
    private readonly ILogger<PaymentToSwiftConverter> _logger;

    public PaymentToSwiftConverter(ILogger<PaymentToSwiftConverter> logger)
    {
        _logger = logger;
    }

    public async Task<SwiftMtxPayment> ConvertToSwiftMtxAsync(
        PaymentRequestDto request,
        Account senderAccount,
        Account receiverAccount,
        Guid transactionId)
    {
        _logger.LogInformation("Converting payment to SwiftMtxPayment format. TransactionId: {TransactionId}", transactionId);

        // Convert payment data to SwiftMtxPayment canonical model
        // This simulates creating an MT103-like structure from payment request
        var mtx = new SwiftMtxPayment
        {
            TransactionId = transactionId.ToString(),
            ValueDate = DateTime.UtcNow.Date, // Use current date as value date
            Amount = request.Amount,
            Currency = request.Currency,
            OrderingCustomer = new Party
            {
                Name = senderAccount.Name,
                Account = $"/{senderAccount.Id}" // Format account as MT103 style
            },
            BeneficiaryCustomer = new Party
            {
                Name = receiverAccount.Name,
                Account = $"/{receiverAccount.Id}" // Format account as MT103 style
            },
            Charges = "OUR", // Default to sender pays charges
            Status = "RECEIVED",
            ReceivedAt = DateTime.UtcNow,
            BankOperationCode = "CRED" // Credit transfer
        };

        _logger.LogInformation("Payment converted to SwiftMtxPayment. Ordering: {OrderingName}, Beneficiary: {BeneficiaryName}",
            mtx.OrderingCustomer.Name, mtx.BeneficiaryCustomer.Name);

        return await Task.FromResult(mtx);
    }
}


using FintechPaymentPOC.Application.DTOs;
using FintechPaymentPOC.Domain.Entities;
using FintechPaymentPOC.Domain.ValueObjects;

namespace FintechPaymentPOC.Application.Interfaces;

/// <summary>
/// Converts payment data (from PaymentRequestDto or Transaction) to SwiftMtxPayment
/// This enables using the MT103 → pacs.008 conversion pipeline for regular payments
/// </summary>
public interface IPaymentToSwiftConverter
{
    /// <summary>
    /// Converts payment request and account information to SwiftMtxPayment
    /// </summary>
    Task<SwiftMtxPayment> ConvertToSwiftMtxAsync(
        PaymentRequestDto request,
        Account senderAccount,
        Account receiverAccount,
        Guid transactionId);
}


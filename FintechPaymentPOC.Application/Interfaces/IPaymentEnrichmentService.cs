using FintechPaymentPOC.Domain.ValueObjects;

namespace FintechPaymentPOC.Application.Interfaces;

/// <summary>
/// Service for enriching and validating MTX payments
/// </summary>
public interface IPaymentEnrichmentService
{
    /// <summary>
    /// Enriches and validates a SwiftMtxPayment
    /// - Generates missing TransactionId
    /// - Sets default Status if missing
    /// - Validates required fields
    /// - Validates account existence
    /// </summary>
    Task<SwiftMtxPayment> EnrichAndValidateAsync(SwiftMtxPayment mtx);
}


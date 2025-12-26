using FintechPaymentPOC.Application.DTOs;
using FintechPaymentPOC.Domain.ValueObjects;

namespace FintechPaymentPOC.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponseDto> CreatePaymentAsync(PaymentRequestDto request);
    Task<bool> ValidatePaymentAsync(PaymentRequestDto request);
    MTXPayment TransformToMTX(Guid transactionId, PaymentRequestDto request, decimal? fraudScore = null);
    Task ProcessPaymentAsync(Guid transactionId);
}


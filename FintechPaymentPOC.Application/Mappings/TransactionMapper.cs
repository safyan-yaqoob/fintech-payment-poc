using FintechPaymentPOC.Application.DTOs;
using FintechPaymentPOC.Domain.Entities;

namespace FintechPaymentPOC.Application.Mappings;

public static class TransactionMapper
{
    public static TransactionDto ToDto(Transaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            SenderAccountId = transaction.SenderAccountId,
            SenderName = transaction.SenderAccount?.Name ?? "Unknown",
            ReceiverAccountId = transaction.ReceiverAccountId,
            ReceiverName = transaction.ReceiverAccount?.Name ?? "Unknown",
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            Status = transaction.Status.ToString(),
            Timestamp = transaction.Timestamp,
            FailureReason = transaction.FailureReason
        };
    }
}


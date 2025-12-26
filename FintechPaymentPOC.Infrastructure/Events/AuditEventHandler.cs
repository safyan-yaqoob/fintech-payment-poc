using FintechPaymentPOC.Domain.Events;
using Microsoft.Extensions.Logging;

namespace FintechPaymentPOC.Infrastructure.Events;

/// <summary>
/// Event handler that logs audit information for payment events
/// </summary>
public class AuditEventHandler : IDomainEventHandler<PaymentRequestedEvent>
{
    private readonly ILogger<AuditEventHandler> _logger;

    public AuditEventHandler(ILogger<AuditEventHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles PaymentRequestedEvent - creates audit log entry
    /// </summary>
    public async Task HandleAsync(PaymentRequestedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "📋 DOMAIN EVENT HANDLER: AuditEventHandler creating audit log entry for Transaction: {TransactionId}",
            domainEvent.TransactionId);

        try
        {
            // Simulate audit logging delay
            await Task.Delay(100, cancellationToken);

            // In a real system, this would write to an audit database or audit service
            _logger.LogInformation(
                "📋 AUDIT LOG ENTRY CREATED - TransactionId: {TransactionId}, OccurredOn: {OccurredOn}, " +
                "Sender: {SenderName} ({SenderAccountId}), Receiver: {ReceiverName} ({ReceiverAccountId}), " +
                "Amount: {Amount} {Currency}, EventType: PaymentRequested",
                domainEvent.TransactionId,
                domainEvent.OccurredOn,
                domainEvent.SenderName,
                domainEvent.SenderAccountId,
                domainEvent.ReceiverName,
                domainEvent.ReceiverAccountId,
                domainEvent.Amount,
                domainEvent.Currency);

            _logger.LogInformation(
                "✅ Audit log entry created successfully for Transaction: {TransactionId}",
                domainEvent.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Error creating audit log entry for Transaction: {TransactionId}",
                domainEvent.TransactionId);
        }
    }
}


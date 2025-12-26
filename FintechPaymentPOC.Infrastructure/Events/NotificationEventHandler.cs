using FintechPaymentPOC.Domain.Events;
using Microsoft.Extensions.Logging;

namespace FintechPaymentPOC.Infrastructure.Events;

/// <summary>
/// Event handler that sends notifications when payment events occur
/// </summary>
public class NotificationEventHandler : IDomainEventHandler<PaymentRequestedEvent>
{
    private readonly ILogger<NotificationEventHandler> _logger;

    public NotificationEventHandler(ILogger<NotificationEventHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles PaymentRequestedEvent - sends notifications to sender and receiver
    /// </summary>
    public async Task HandleAsync(PaymentRequestedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "📧 DOMAIN EVENT HANDLER: NotificationEventHandler processing notifications for Transaction: {TransactionId}",
            domainEvent.TransactionId);

        try
        {
            // Simulate sending notification to sender
            await Task.Delay(200, cancellationToken);
            _logger.LogInformation(
                "📨 Notification sent to SENDER - Name: {SenderName}, Account: {SenderAccountId}, Message: 'Payment of {Amount} {Currency} initiated to {ReceiverName}'",
                domainEvent.SenderName,
                domainEvent.SenderAccountId,
                domainEvent.Amount,
                domainEvent.Currency,
                domainEvent.ReceiverName);

            // Simulate sending notification to receiver
            await Task.Delay(200, cancellationToken);
            _logger.LogInformation(
                "📨 Notification sent to RECEIVER - Name: {ReceiverName}, Account: {ReceiverAccountId}, Message: 'Incoming payment of {Amount} {Currency} from {SenderName}'",
                domainEvent.ReceiverName,
                domainEvent.ReceiverAccountId,
                domainEvent.Amount,
                domainEvent.Currency,
                domainEvent.SenderName);

            _logger.LogInformation(
                "✅ Notifications sent successfully for Transaction: {TransactionId}",
                domainEvent.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Error sending notifications for Transaction: {TransactionId}",
                domainEvent.TransactionId);
        }
    }
}


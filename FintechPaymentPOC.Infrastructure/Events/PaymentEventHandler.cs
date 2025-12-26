using FintechPaymentPOC.Application.Interfaces;
using FintechPaymentPOC.Domain.Entities;
using FintechPaymentPOC.Domain.Events;
using FintechPaymentPOC.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FintechPaymentPOC.Infrastructure.Events;

/// <summary>
/// Event handler that processes payment events and updates account balances
/// </summary>
public class PaymentEventHandler : IDomainEventHandler<PaymentRequestedEvent>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PaymentEventHandler> _logger;

    public PaymentEventHandler(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PaymentEventHandler> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Handles PaymentRequestedEvent - processes the payment and updates account balances
    /// </summary>
    public async Task HandleAsync(PaymentRequestedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "🔔 DOMAIN EVENT HANDLER: PaymentRequestedEvent received - Transaction: {TransactionId}, Amount: {Amount} {Currency}, From: {SenderName} ({SenderAccountId}) To: {ReceiverName} ({ReceiverAccountId})",
            domainEvent.TransactionId,
            domainEvent.Amount,
            domainEvent.Currency,
            domainEvent.SenderName,
            domainEvent.SenderAccountId,
            domainEvent.ReceiverName,
            domainEvent.ReceiverAccountId);

        // Create a scope for this event processing to access scoped services
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

        // NOTE: We do NOT create a new transaction here because:
        // 1. This handler is called from SaveChangesAsync which is already in a transaction
        // 2. All changes made here will be part of the parent transaction
        // 3. If this handler fails, the parent transaction will rollback everything
        // 4. We just need to make changes and call SaveChangesAsync - it will use the existing transaction
        
        try
        {
            _logger.LogInformation("🔄 Processing payment within existing database transaction");

            // Get the transaction
            var transaction = await repository.GetTransactionByIdAsync(domainEvent.TransactionId);
            if (transaction == null)
            {
                _logger.LogError("Transaction not found: {TransactionId}", domainEvent.TransactionId);
                throw new InvalidOperationException($"Transaction {domainEvent.TransactionId} not found");
            }

            // Check if already processed
            if (transaction.Status == TransactionStatus.Completed)
            {
                _logger.LogWarning("Transaction {TransactionId} already completed, skipping processing", domainEvent.TransactionId);
                return;
            }

            // Update status to Processing
            transaction.Status = TransactionStatus.Processing;
            await repository.UpdateTransactionAsync(transaction);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("📊 Transaction {TransactionId} status updated to Processing", domainEvent.TransactionId);

            // Get accounts
            var senderAccount = await repository.GetAccountByIdAsync(domainEvent.SenderAccountId);
            var receiverAccount = await repository.GetAccountByIdAsync(domainEvent.ReceiverAccountId);

            if (senderAccount == null || receiverAccount == null)
            {
                throw new InvalidOperationException($"One or both accounts not found. Sender: {domainEvent.SenderAccountId}, Receiver: {domainEvent.ReceiverAccountId}");
            }

            // Log current balances
            _logger.LogInformation(
                "💰 Current balances - Sender ({SenderName}): {SenderBalance} {Currency}, Receiver ({ReceiverName}): {ReceiverBalance} {Currency}",
                senderAccount.Name,
                senderAccount.Balance,
                domainEvent.Currency,
                receiverAccount.Name,
                receiverAccount.Balance,
                domainEvent.Currency);

            // Validate sender has sufficient balance
            if (senderAccount.Balance < domainEvent.Amount)
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.FailureReason = $"Insufficient balance. Available: {senderAccount.Balance}, Required: {domainEvent.Amount}";
                await repository.UpdateTransactionAsync(transaction);
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogError(
                    "❌ Payment failed - Insufficient balance. Sender ({SenderName}) has {Balance} {Currency}, but {Amount} {Currency} is required",
                    senderAccount.Name,
                    senderAccount.Balance,
                    domainEvent.Currency,
                    domainEvent.Amount,
                    domainEvent.Currency);
                // Throw exception to cause parent transaction to rollback
                throw new InvalidOperationException($"Insufficient balance. Available: {senderAccount.Balance}, Required: {domainEvent.Amount}");
            }

            // Simulate processing delay
            await Task.Delay(300, cancellationToken);
            _logger.LogInformation("⏳ Simulating payment processing delay...");

            // Update balances within transaction
            var newSenderBalance = senderAccount.Balance - domainEvent.Amount;
            var newReceiverBalance = receiverAccount.Balance + domainEvent.Amount;

            await repository.UpdateAccountBalanceAsync(domainEvent.SenderAccountId, newSenderBalance);
            await repository.UpdateAccountBalanceAsync(domainEvent.ReceiverAccountId, newReceiverBalance);

            _logger.LogInformation(
                "💰 Account balances updated - Sender ({SenderName}): {OldBalance} → {NewBalance} {Currency}, Receiver ({ReceiverName}): {OldReceiverBalance} → {NewReceiverBalance} {Currency}",
                senderAccount.Name,
                senderAccount.Balance,
                newSenderBalance,
                domainEvent.Currency,
                receiverAccount.Name,
                receiverAccount.Balance,
                newReceiverBalance,
                domainEvent.Currency);

            // Update transaction status to Completed
            transaction.Status = TransactionStatus.Completed;
            await repository.UpdateTransactionAsync(transaction);
            
            // Save changes - this will be part of the parent transaction from SaveChangesAsync
            // The parent transaction will commit after all handlers succeed
            await dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "✅ Payment processing completed successfully - Transaction: {TransactionId}, Amount: {Amount} {Currency}. Changes saved (will be committed with parent transaction).",
                domainEvent.TransactionId,
                domainEvent.Amount,
                domainEvent.Currency);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Error processing payment event - Transaction: {TransactionId}. Exception will cause parent transaction to rollback.",
                domainEvent.TransactionId);

            // Update transaction status to Failed before rethrowing
            // This will be rolled back if the parent transaction rolls back
            try
            {
                var transaction = await repository.GetTransactionByIdAsync(domainEvent.TransactionId);
                if (transaction != null)
                {
                    transaction.Status = TransactionStatus.Failed;
                    transaction.FailureReason = ex.Message;
                    await repository.UpdateTransactionAsync(transaction);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update transaction status after error");
            }

            // Re-throw exception so parent transaction can rollback
            throw;
        }
    }
}


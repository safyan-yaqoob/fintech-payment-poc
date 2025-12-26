using FintechPaymentPOC.Application.DTOs;
using FintechPaymentPOC.Application.Interfaces;
using FintechPaymentPOC.Domain.Entities;
using FintechPaymentPOC.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FintechPaymentPOC.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repository;
    private readonly IPaymentToSwiftConverter _paymentToSwiftConverter;
    private readonly IPaymentEnrichmentService _enrichmentService;
    private readonly IPacs008Generator _pacs008Generator;
    private readonly ILogger<PaymentService> _logger;
    private static readonly string[] ValidCurrencies = { "USD", "EUR", "GBP", "JPY" , "AED" };

    public PaymentService(
        IPaymentRepository repository,
        IPaymentToSwiftConverter paymentToSwiftConverter,
        IPaymentEnrichmentService enrichmentService,
        IPacs008Generator pacs008Generator,
        ILogger<PaymentService> logger)
    {
        _repository = repository;
        _paymentToSwiftConverter = paymentToSwiftConverter;
        _enrichmentService = enrichmentService;
        _pacs008Generator = pacs008Generator;
        _logger = logger;
    }

    public async Task<PaymentResponseDto> CreatePaymentAsync(PaymentRequestDto request)
    {
        _logger.LogInformation("Creating payment: {Amount} {Currency} from {Sender} to {Receiver}",
            request.Amount, request.Currency, request.SenderAccountId, request.ReceiverAccountId);

        // Validate payment
        var isValid = await ValidatePaymentAsync(request);
        if (!isValid)
        {
            return new PaymentResponseDto
            {
                Status = "Failed",
                Message = "Payment validation failed"
            };
        }

        // Create transaction
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            SenderAccountId = request.SenderAccountId,
            ReceiverAccountId = request.ReceiverAccountId,
            Amount = request.Amount,
            Currency = request.Currency,
            Status = TransactionStatus.Pending,
            Timestamp = DateTime.UtcNow
        };

        // Get account names for domain event
        var senderAccount = await _repository.GetAccountByIdAsync(request.SenderAccountId);
        var receiverAccount = await _repository.GetAccountByIdAsync(request.ReceiverAccountId);

        if (senderAccount == null || receiverAccount == null)
        {
            return new PaymentResponseDto
            {
                Status = "Failed",
                Message = "One or both accounts not found"
            };
        }

        // Convert payment to SwiftMtxPayment and generate pacs.008 XML
        // This integrates MT103 → pacs.008 conversion into the payment flow
        _logger.LogInformation("Converting payment to SwiftMtxPayment and generating pacs.008 XML");
        
        var swiftMtx = await _paymentToSwiftConverter.ConvertToSwiftMtxAsync(
            request, senderAccount, receiverAccount, transaction.Id);
        
        // Enrich and validate the SwiftMtxPayment
        swiftMtx = await _enrichmentService.EnrichAndValidateAsync(swiftMtx);
        
        // Generate pacs.008 XML
        swiftMtx.Status = "TRANSFORMED";
        swiftMtx.TransformedAt = DateTime.UtcNow;
        var pacs008Xml = _pacs008Generator.Generate(swiftMtx);
        
        // Store pacs.008 XML in transaction
        transaction.Pacs008Xml = pacs008Xml;
        _logger.LogInformation("pacs.008 XML generated and stored in transaction. Length: {Length} characters", pacs008Xml.Length);

        // Raise domain event on the transaction entity
        // This event will be dispatched when SaveChangesAsync is called
        transaction.RequestPayment(
            senderAccount.Name,
            receiverAccount.Name);

        // Save transaction - this will trigger domain event dispatch
        transaction = await _repository.CreateTransactionAsync(transaction);
        _logger.LogInformation("Transaction created with domain event and pacs.008 XML. Event will be dispatched on SaveChangesAsync.");

        // Transform to MTX with fraud score
        var fraudScore = await SimulateFraudDetectionAsync(request);
        var mtxPayment = TransformToMTX(transaction.Id, request, fraudScore);

        // Domain events are dispatched automatically by DbContext.SaveChangesAsync
        // Wait a bit for handlers to complete
        await Task.Delay(500);
        var finalTransaction = await _repository.GetTransactionByIdAsync(transaction.Id);
        
        if (finalTransaction == null)
        {
            return new PaymentResponseDto
            {
                TransactionId = transaction.Id,
                Status = "Failed",
                MTXPayment = mtxPayment,
                Message = "Transaction not found after processing"
            };
        }

        // Return appropriate response based on transaction status
        var status = finalTransaction.Status switch
        {
            TransactionStatus.Completed => "Completed",
            TransactionStatus.Failed => "Failed",
            TransactionStatus.Processing => "Processing",
            _ => "Pending"
        };

        return new PaymentResponseDto
        {
            TransactionId = transaction.Id,
            Status = status,
            MTXPayment = mtxPayment,
            Pacs008Xml = finalTransaction.Pacs008Xml, // Include pacs.008 XML in response
            Message = finalTransaction.Status == TransactionStatus.Completed
                ? "Payment processed successfully with pacs.008 conversion"
                : finalTransaction.Status == TransactionStatus.Failed
                    ? $"Payment failed: {finalTransaction.FailureReason ?? "Unknown error"}"
                    : "Payment is being processed"
        };
    }

    public async Task<bool> ValidatePaymentAsync(PaymentRequestDto request)
    {
        // Validate amount
        if (request.Amount <= 0)
        {
            _logger.LogWarning("Invalid amount: {Amount}", request.Amount);
            return false;
        }

        // Validate currency
        if (!ValidCurrencies.Contains(request.Currency.ToUpper()))
        {
            _logger.LogWarning("Invalid currency: {Currency}", request.Currency);
            return false;
        }

        // Validate accounts exist
        var senderAccount = await _repository.GetAccountByIdAsync(request.SenderAccountId);
        if (senderAccount == null)
        {
            _logger.LogWarning("Sender account not found: {AccountId}", request.SenderAccountId);
            return false;
        }

        var receiverAccount = await _repository.GetAccountByIdAsync(request.ReceiverAccountId);
        if (receiverAccount == null)
        {
            _logger.LogWarning("Receiver account not found: {AccountId}", request.ReceiverAccountId);
            return false;
        }

        // Validate sender has enough balance
        if (senderAccount.Balance < request.Amount)
        {
            _logger.LogWarning("Insufficient balance. Account: {AccountId}, Balance: {Balance}, Required: {Amount}",
                request.SenderAccountId, senderAccount.Balance, request.Amount);
            return false;
        }

        // Validate accounts are different
        if (request.SenderAccountId == request.ReceiverAccountId)
        {
            _logger.LogWarning("Sender and receiver accounts cannot be the same");
            return false;
        }

        // Fraud detection simulation
        var fraudScore = await SimulateFraudDetectionAsync(request);
        if (fraudScore > 0.8m)
        {
            _logger.LogWarning("High fraud score detected: {Score}", fraudScore);
            return false;
        }

        return true;
    }

    public MTXPayment TransformToMTX(Guid transactionId, PaymentRequestDto request, decimal? fraudScore = null)
    {
        return new MTXPayment
        {
            TransactionId = transactionId,
            Sender = request.SenderAccountId.ToString(),
            Receiver = request.ReceiverAccountId.ToString(),
            Amount = request.Amount,
            Currency = request.Currency,
            Timestamp = DateTime.UtcNow,
            Status = "Pending",
            FraudScore = fraudScore
        };
    }

    public async Task ProcessPaymentAsync(Guid transactionId)
    {
        var transaction = await _repository.GetTransactionByIdAsync(transactionId);
        if (transaction == null)
        {
            _logger.LogError("Transaction not found: {TransactionId}", transactionId);
            return;
        }

        try
        {
            // Update status to Processing
            transaction.Status = TransactionStatus.Processing;
            await _repository.UpdateTransactionAsync(transaction);

            // Get accounts
            var senderAccount = await _repository.GetAccountByIdAsync(transaction.SenderAccountId);
            var receiverAccount = await _repository.GetAccountByIdAsync(transaction.ReceiverAccountId);

            if (senderAccount == null || receiverAccount == null)
            {
                throw new InvalidOperationException("One or both accounts not found");
            }

            // Update balances
            var newSenderBalance = senderAccount.Balance - transaction.Amount;
            var newReceiverBalance = receiverAccount.Balance + transaction.Amount;

            await _repository.UpdateAccountBalanceAsync(transaction.SenderAccountId, newSenderBalance);
            await _repository.UpdateAccountBalanceAsync(transaction.ReceiverAccountId, newReceiverBalance);

            // Update transaction status to Completed
            transaction.Status = TransactionStatus.Completed;
            await _repository.UpdateTransactionAsync(transaction);

            _logger.LogInformation("Payment processed successfully. Transaction: {TransactionId}", transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment. Transaction: {TransactionId}", transactionId);
            transaction.Status = TransactionStatus.Failed;
            transaction.FailureReason = ex.Message;
            await _repository.UpdateTransactionAsync(transaction);
        }
    }

    private async Task<decimal> SimulateFraudDetectionAsync(PaymentRequestDto request)
    {
        // Simulate AI fraud detection - returns a random score between 0 and 1
        await Task.Delay(50); // Simulate processing time
        var random = new Random();
        return (decimal)random.NextDouble();
    }
}


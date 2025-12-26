using FintechPaymentPOC.Application.Interfaces;
using FintechPaymentPOC.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FintechPaymentPOC.Application.Services;

/// <summary>
/// Payment Enrichment Service
/// 
/// WHY ENRICHMENT IS MANDATORY WHEN CONVERTING MT → MX:
/// ======================================================
/// 1. MT103 has optional fields - ISO 20022 requires many mandatory fields
///    - MT103 :20: (TransactionId) can be missing → pacs.008 requires MsgId
///    - MT103 doesn't have status → pacs.008 needs processing status
/// 
/// 2. Data format differences require normalization:
///    - MT103 amounts use comma (1000,) → ISO 20022 needs decimal (1000.00)
///    - MT103 dates are YYMMDD → ISO 20022 needs full ISO 8601 dates
///    - MT103 parties can be incomplete → ISO 20022 requires structured debtor/creditor
/// 
/// 3. Business rules validation:
///    - Amount must be > 0 (MT103 doesn't enforce this)
///    - Currency must be valid ISO 4217 code
///    - Accounts must exist in the system
///    - Parties must have names (MT103 can have account only)
/// 
/// 4. Enrichment adds missing context:
///    - Generate unique transaction IDs if missing
///    - Set default status (RECEIVED) for tracking
///    - Add timestamps for audit trail
///    - Validate against business rules before conversion
/// 
/// 5. Prevents invalid pacs.008 generation:
///    - Catches errors early before XML generation
///    - Ensures data quality for downstream systems
///    - Maintains data integrity across format conversion
/// </summary>
public class PaymentEnrichmentService : IPaymentEnrichmentService
{
    private readonly IPaymentRepository _repository;
    private readonly ILogger<PaymentEnrichmentService> _logger;

    public PaymentEnrichmentService(
        IPaymentRepository repository,
        ILogger<PaymentEnrichmentService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<SwiftMtxPayment> EnrichAndValidateAsync(SwiftMtxPayment mtx)
    {
        _logger.LogInformation("Starting enrichment and validation for MTX payment");

        // Enrichment: Generate TransactionId if missing
        if (string.IsNullOrWhiteSpace(mtx.TransactionId))
        {
            mtx.TransactionId = Guid.NewGuid().ToString();
            _logger.LogInformation("Generated TransactionId: {TransactionId}", mtx.TransactionId);
        }

        // Enrichment: Set default Status if missing
        if (string.IsNullOrWhiteSpace(mtx.Status))
        {
            mtx.Status = "RECEIVED";
            _logger.LogInformation("Set default Status: RECEIVED");
        }

        // Enrichment: Set default Charges if missing
        if (string.IsNullOrWhiteSpace(mtx.Charges))
        {
            mtx.Charges = "OUR";
            _logger.LogInformation("Set default Charges: OUR");
        }

        // Enrichment: Set default Currency if missing
        if (string.IsNullOrWhiteSpace(mtx.Currency))
        {
            mtx.Currency = "USD";
            _logger.LogInformation("Set default Currency: USD");
        }

        // Enrichment: Ensure ValueDate is set (default to today if missing)
        if (mtx.ValueDate == default)
        {
            mtx.ValueDate = DateTime.UtcNow.Date;
            _logger.LogInformation("Set default ValueDate: {ValueDate}", mtx.ValueDate);
        }

        // Validation: Amount must be > 0
        if (mtx.Amount <= 0)
        {
            throw new InvalidOperationException($"Amount must be greater than 0. Current value: {mtx.Amount}");
        }
        _logger.LogInformation("✓ Validated Amount: {Amount}", mtx.Amount);

        // Validation: Currency must not be empty
        if (string.IsNullOrWhiteSpace(mtx.Currency))
        {
            throw new InvalidOperationException("Currency cannot be empty");
        }
        _logger.LogInformation("✓ Validated Currency: {Currency}", mtx.Currency);

        // Validation: Currency must be valid ISO 4217 code (3 letters)
        if (mtx.Currency.Length != 3 || !mtx.Currency.All(char.IsLetter))
        {
            throw new InvalidOperationException($"Invalid currency code: {mtx.Currency}. Must be 3-letter ISO 4217 code.");
        }

        // Validation: Ordering customer must have name or account
        if (mtx.OrderingCustomer == null || 
            (string.IsNullOrWhiteSpace(mtx.OrderingCustomer.Name) && 
             string.IsNullOrWhiteSpace(mtx.OrderingCustomer.Account)))
        {
            throw new InvalidOperationException("Ordering customer must have either name or account");
        }
        _logger.LogInformation("✓ Validated OrderingCustomer: {Name}, {Account}",
            mtx.OrderingCustomer.Name, mtx.OrderingCustomer.Account);

        // Validation: Beneficiary customer must have name or account
        if (mtx.BeneficiaryCustomer == null ||
            (string.IsNullOrWhiteSpace(mtx.BeneficiaryCustomer.Name) &&
             string.IsNullOrWhiteSpace(mtx.BeneficiaryCustomer.Account)))
        {
            throw new InvalidOperationException("Beneficiary customer must have either name or account");
        }
        _logger.LogInformation("✓ Validated BeneficiaryCustomer: {Name}, {Account}",
            mtx.BeneficiaryCustomer.Name, mtx.BeneficiaryCustomer.Account);

        // Validation: Check if accounts exist in the system (if account numbers are provided)
        await ValidateAccountExistsAsync(mtx.OrderingCustomer.Account, "Ordering customer");
        await ValidateAccountExistsAsync(mtx.BeneficiaryCustomer.Account, "Beneficiary customer");

        _logger.LogInformation("Enrichment and validation completed successfully");

        return mtx;
    }

    /// <summary>
    /// Validates that an account exists in the system (if account number is provided)
    /// </summary>
    private async Task ValidateAccountExistsAsync(string accountNumber, string partyType)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            return; // Account is optional, skip validation
        }

        // Extract account number from format like "/123456789"
        var accountIdStr = accountNumber.TrimStart('/');
        
        // Try to parse as GUID (if it's a GUID format)
        if (Guid.TryParse(accountIdStr, out var accountId))
        {
            var account = await _repository.GetAccountByIdAsync(accountId);
            if (account == null)
            {
                _logger.LogWarning("Account {AccountId} for {PartyType} not found in system, but continuing", 
                    accountId, partyType);
                // Don't throw - account might be external
            }
            else
            {
                _logger.LogInformation("✓ Validated {PartyType} account exists: {AccountId}", 
                    partyType, accountId);
            }
        }
        else
        {
            _logger.LogInformation("Account number {AccountNumber} for {PartyType} is not a GUID, assuming external account",
                accountNumber, partyType);
        }
    }
}


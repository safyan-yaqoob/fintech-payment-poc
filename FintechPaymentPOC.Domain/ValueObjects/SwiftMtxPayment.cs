namespace FintechPaymentPOC.Domain.ValueObjects;

/// <summary>
/// MTX (Message Transfer eXchange) Canonical Model for SWIFT MT103 → ISO 20022 conversion
/// 
/// WHY MTX EXISTS:
/// ================
/// 1. MT103 (SWIFT) and pacs.008 (ISO 20022) have fundamentally different data models:
///    - MT103 uses tag-based format (:20:, :32A:, etc.) with limited structure
///    - ISO 20022 uses rich XML schemas with nested elements and relationships
///    - Direct mapping is impossible due to semantic gaps
/// 
/// 2. MTX serves as a canonical/neutral data model that:
///    - Captures all relevant payment information from MT103
///    - Enriches data with defaults and validations
///    - Provides a clean structure for generating ISO 20022 messages
///    - Acts as a bridge between legacy SWIFT and modern ISO 20022 formats
/// 
/// 3. WHY MTX IS RICHER THAN MT103:
///    - MT103 has flat, tag-based structure with limited validation
///    - MTX adds structured objects (Party, dates, amounts)
///    - MTX includes status tracking (RECEIVED, TRANSFORMED, SENT)
///    - MTX supports enrichment (generated IDs, defaults, validations)
///    - MTX normalizes data formats (dates, amounts, currencies)
/// 
/// 4. BANKS MUST SUPPORT BOTH FORMATS:
///    - Legacy systems still use MT103 (SWIFT)
///    - New systems require ISO 20022 (pacs.008)
///    - MTX enables seamless conversion between both formats
///    - Allows gradual migration from MT to MX without breaking existing systems
/// </summary>
public class SwiftMtxPayment
{
    /// <summary>
    /// Transaction reference from MT103 :20: tag
    /// If missing, will be auto-generated during enrichment
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Value date from MT103 :32A: tag (format: YYMMDD)
    /// Represents the date when funds should be available
    /// </summary>
    public DateTime ValueDate { get; set; }

    /// <summary>
    /// Amount from MT103 :32A: tag
    /// Format in MT103: "1000," (comma as decimal separator)
    /// Converted to decimal: 1000.00
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code from MT103 :32A: tag (e.g., USD, EUR, GBP)
    /// ISO 4217 three-letter currency code
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Ordering customer from MT103 :50K: tag
    /// The party initiating the payment (sender/debtor)
    /// </summary>
    public Party OrderingCustomer { get; set; } = new Party();

    /// <summary>
    /// Beneficiary customer from MT103 :59: tag
    /// The party receiving the payment (receiver/creditor)
    /// </summary>
    public Party BeneficiaryCustomer { get; set; } = new Party();

    /// <summary>
    /// Charges indicator from MT103 :71A: tag
    /// Values: OUR (sender pays), BEN (beneficiary pays), SHA (shared)
    /// </summary>
    public string Charges { get; set; } = "OUR";

    /// <summary>
    /// Processing status in the payment hub lifecycle
    /// Values: RECEIVED → TRANSFORMED → SENT
    /// </summary>
    public string Status { get; set; } = "RECEIVED";

    /// <summary>
    /// Timestamp when the MT103 was received
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when conversion to pacs.008 was completed
    /// </summary>
    public DateTime? TransformedAt { get; set; }

    /// <summary>
    /// Timestamp when pacs.008 was sent
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Additional transaction reference from MT103 :23B: tag (optional)
    /// Bank operation code (e.g., CRED for credit transfer)
    /// </summary>
    public string? BankOperationCode { get; set; }
}


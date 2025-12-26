# SWIFT MT103 → ISO 20022 pacs.008 Conversion

## Conceptual Overview

### Why MT Cannot Be Directly Mapped to MX

**MT103 (SWIFT) Characteristics:**
- Tag-based format with flat structure (`:20:`, `:32A:`, `:50K:`, etc.)
- Optional fields - many tags can be missing
- Limited validation - format validation only, no business rules
- Legacy format designed for telex/telegram era
- Fixed field positions and formats
- No hierarchical relationships between data elements

**pacs.008 (ISO 20022) Characteristics:**
- XML-based with rich hierarchical structure
- Mandatory fields - many elements are required
- Strong validation - XML schema + business rules
- Modern format designed for structured data exchange
- Flexible and extensible schema
- Rich relationships and nested elements

**The Gap:**
- MT103 `:20:` (Transaction Reference) is optional → pacs.008 `MsgId` is mandatory
- MT103 has no status field → pacs.008 requires processing status
- MT103 parties are flat text → pacs.008 requires structured `Dbtr`/`Cdtr` objects
- MT103 amounts use comma (`1000,`) → pacs.008 requires decimal (`1000.00`)
- MT103 dates are YYMMDD → pacs.008 requires ISO 8601 full dates
- MT103 has no validation → pacs.008 requires account existence checks

### Why MTX Is Required

**MTX (Message Transfer eXchange) Canonical Model:**

MTX serves as an intermediate, neutral data model that:

1. **Bridges the Semantic Gap:**
   - Captures all relevant information from MT103
   - Structures data in a way that maps cleanly to ISO 20022
   - Normalizes formats (dates, amounts, currencies)

2. **Enables Enrichment:**
   - Adds missing mandatory fields (TransactionId, Status)
   - Applies business rules and validations
   - Enriches with defaults and computed values

3. **Provides Abstraction:**
   - Decouples MT103 parsing from pacs.008 generation
   - Allows multiple output formats (pacs.008, pacs.009, etc.)
   - Enables testing and validation at each stage

4. **Supports Transformation Pipeline:**
   ```
   MT103 → Parse → MTX → Enrich → Validate → Transform → pacs.008
   ```

### Why Banks Must Support Both Formats

**Legacy Systems:**
- Many banks still use SWIFT MT messages internally
- Legacy systems expect MT format
- Migration is gradual, not instant

**Modern Systems:**
- New payment systems require ISO 20022
- Regulatory requirements (e.g., SEPA) mandate ISO 20022
- Cross-border payments moving to ISO 20022

**Interoperability:**
- Banks receive MT103 from legacy systems
- Banks must send pacs.008 to modern systems
- Conversion enables seamless integration

**Gradual Migration:**
- Banks can migrate incrementally
- MTX allows supporting both formats simultaneously
- Reduces risk during transition period

## Conversion Flow

```
┌─────────────┐
│   MT103     │  Raw SWIFT message text
│   Input     │  :20:TRX123456
└──────┬──────┘  :32A:250925USD1000,
       │         :50K:/123456789
       │         JOHN DOE
       ▼         :59:/987654321
┌─────────────┐  JANE DOE
│   Parse     │  :71A:OUR
│   MT103     │
└──────┬──────┘
       │
       ▼
┌─────────────┐
│     MTX     │  Canonical model
│   Model     │  - TransactionId
└──────┬──────┘  - ValueDate, Amount, Currency
       │         - OrderingCustomer (Party)
       │         - BeneficiaryCustomer (Party)
       ▼         - Charges, Status
┌─────────────┐
│  Enrich &   │  - Generate missing IDs
│  Validate   │  - Set defaults
└──────┬──────┘  - Validate business rules
       │         - Check account existence
       ▼
┌─────────────┐
│  Transform  │  Map MTX → pacs.008 XML
│  to pacs.008│  - GroupHeader
└──────┬──────┘  - CreditTransferTransactionInformation
       │         - Amount, Debtor, Creditor
       ▼
┌─────────────┐
│  pacs.008   │  ISO 20022 XML output
│   Output    │  Ready for payment processing
└─────────────┘
```

## Status Lifecycle

1. **RECEIVED**: MT103 received and parsed into MTX
2. **TRANSFORMED**: MTX converted to pacs.008 XML
3. **SENT**: pacs.008 sent to destination system

Each status transition is logged for audit and tracking purposes.


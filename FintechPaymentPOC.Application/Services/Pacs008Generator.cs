using FintechPaymentPOC.Application.Interfaces;
using FintechPaymentPOC.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;

namespace FintechPaymentPOC.Application.Services;

/// <summary>
/// Pacs.008 Generator Service
/// 
/// WHAT IS PACS.008:
/// =================
/// - Customer Credit Transfer message in ISO 20022 format
/// - XML-based message standard for payment instructions
/// - Used by modern payment systems (SEPA, Faster Payments, etc.)
/// - Replaces legacy SWIFT MT103 format in many jurisdictions
/// 
/// REQUIRED XML ELEMENTS:
/// ======================
/// - GroupHeader: Message identification and control information
///   - MsgId: Unique message identifier
///   - CreDtTm: Creation date/time
///   - NbOfTxs: Number of transactions
/// 
/// - CreditTransferTransactionInformation: Individual payment instruction
///   - InstdAmt: Instructed amount (currency + value)
///   - Dbtr: Debtor (ordering customer) information
///   - DbtrAcct: Debtor account details
///   - Cdtr: Creditor (beneficiary customer) information
///   - CdtrAcct: Creditor account details
///   - RmtInf: Remittance information (optional)
/// 
/// MAPPING FROM MTX TO PACS.008:
/// =============================
/// - SwiftMtxPayment.TransactionId → GroupHeader/MsgId
/// - SwiftMtxPayment.Amount → CreditTransferTransactionInformation/InstdAmt/InstdAmt
/// - SwiftMtxPayment.Currency → CreditTransferTransactionInformation/InstdAmt/Ccy
/// - SwiftMtxPayment.OrderingCustomer → CreditTransferTransactionInformation/Dbtr
/// - SwiftMtxPayment.BeneficiaryCustomer → CreditTransferTransactionInformation/Cdtr
/// </summary>
public class Pacs008Generator : IPacs008Generator
{
    private readonly ILogger<Pacs008Generator> _logger;
    private const string Pacs008Namespace = "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.12";

    public Pacs008Generator(ILogger<Pacs008Generator> logger)
    {
        _logger = logger;
    }

    public string Generate(SwiftMtxPayment mtx)
    {
        _logger.LogInformation("Generating pacs.008 XML for TransactionId: {TransactionId}", mtx.TransactionId);

        // Create root element with namespace
        var document = new XDocument(
            new XElement(XName.Get("Document", Pacs008Namespace),
                new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                new XAttribute(XName.Get("schemaLocation", "http://www.w3.org/2001/XMLSchema-instance"),
                    "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.12 pacs.008.001.12.xsd"),
                new XElement(XName.Get("FIToFICstmrCdtTrf", Pacs008Namespace),
                    // Group Header
                    CreateGroupHeader(mtx),
                    // Credit Transfer Transaction Information
                    CreateCreditTransferTransactionInformation(mtx)
                )
            )
        );

        var xmlString = document.ToString();
        _logger.LogInformation("pacs.008 XML generated successfully. Length: {Length} characters", xmlString.Length);

        return xmlString;
    }

    /// <summary>
    /// Creates GroupHeader element with message identification
    /// </summary>
    private XElement CreateGroupHeader(SwiftMtxPayment mtx)
    {
        return new XElement(XName.Get("GrpHdr", Pacs008Namespace),
            new XElement(XName.Get("MsgId", Pacs008Namespace), mtx.TransactionId),
            new XElement(XName.Get("CreDtTm", Pacs008Namespace), DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
            new XElement(XName.Get("NbOfTxs", Pacs008Namespace), "1"),
            new XElement(XName.Get("CtrlSum", Pacs008Namespace), mtx.Amount.ToString("F2")),
            new XElement(XName.Get("InitgPty", Pacs008Namespace),
                new XElement(XName.Get("Nm", Pacs008Namespace), mtx.OrderingCustomer.Name ?? "Unknown")
            )
        );
    }

    /// <summary>
    /// Creates CreditTransferTransactionInformation element with payment details
    /// </summary>
    private XElement CreateCreditTransferTransactionInformation(SwiftMtxPayment mtx)
    {
        return new XElement(XName.Get("CdtTrfTxInf", Pacs008Namespace),
            // Payment Identification
            new XElement(XName.Get("PmtId", Pacs008Namespace),
                new XElement(XName.Get("EndToEndId", Pacs008Namespace), mtx.TransactionId),
                new XElement(XName.Get("TxId", Pacs008Namespace), mtx.TransactionId)
            ),
            // Instructed Amount
            new XElement(XName.Get("InstdAmt", Pacs008Namespace),
                new XAttribute("Ccy", mtx.Currency),
                mtx.Amount.ToString("F2")
            ),
            // Debtor (Ordering Customer)
            CreateDebtor(mtx.OrderingCustomer),
            // Debtor Account
            CreateDebtorAccount(mtx.OrderingCustomer),
            // Creditor (Beneficiary Customer)
            CreateCreditor(mtx.BeneficiaryCustomer),
            // Creditor Account
            CreateCreditorAccount(mtx.BeneficiaryCustomer),
            // Remittance Information (optional)
            new XElement(XName.Get("RmtInf", Pacs008Namespace),
                new XElement(XName.Get("Ustrd", Pacs008Namespace), 
                    $"MT103 conversion - Charges: {mtx.Charges}, BankOpCode: {mtx.BankOperationCode ?? "N/A"}")
            )
        );
    }

    /// <summary>
    /// Creates Debtor (ordering customer) element
    /// </summary>
    private XElement CreateDebtor(Party orderingCustomer)
    {
        return new XElement(XName.Get("Dbtr", Pacs008Namespace),
            new XElement(XName.Get("Nm", Pacs008Namespace), orderingCustomer.Name ?? "Unknown")
        );
    }

    /// <summary>
    /// Creates Debtor Account element
    /// </summary>
    private XElement CreateDebtorAccount(Party orderingCustomer)
    {
        var accountElement = new XElement(XName.Get("DbtrAcct", Pacs008Namespace),
            new XElement(XName.Get("Id", Pacs008Namespace),
                new XElement(XName.Get("Othr", Pacs008Namespace),
                    new XElement(XName.Get("Id", Pacs008Namespace), 
                        orderingCustomer.Account.TrimStart('/') ?? "N/A")
                )
            )
        );

        return accountElement;
    }

    /// <summary>
    /// Creates Creditor (beneficiary customer) element
    /// </summary>
    private XElement CreateCreditor(Party beneficiaryCustomer)
    {
        return new XElement(XName.Get("Cdtr", Pacs008Namespace),
            new XElement(XName.Get("Nm", Pacs008Namespace), beneficiaryCustomer.Name ?? "Unknown")
        );
    }

    /// <summary>
    /// Creates Creditor Account element
    /// </summary>
    private XElement CreateCreditorAccount(Party beneficiaryCustomer)
    {
        return new XElement(XName.Get("CdtrAcct", Pacs008Namespace),
            new XElement(XName.Get("Id", Pacs008Namespace),
                new XElement(XName.Get("Othr", Pacs008Namespace),
                    new XElement(XName.Get("Id", Pacs008Namespace), 
                        beneficiaryCustomer.Account.TrimStart('/') ?? "N/A")
                )
            )
        );
    }
}


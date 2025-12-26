using FintechPaymentPOC.Application.Interfaces;
using FintechPaymentPOC.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace FintechPaymentPOC.Application.Services;

/// <summary>
/// MT103 Parser Service
/// 
/// RESPONSIBILITIES:
/// - Accept raw MT103 string input
/// - Parse SWIFT tags (:20:, :32A:, :50K:, :59:, :71A:, :23B:)
/// - Extract and normalize data (dates, amounts, parties)
/// - Convert parsed data into SwiftMtxPayment canonical model
/// 
/// MT103 TAG REFERENCE:
/// - :20: Transaction reference (unique identifier)
/// - :23B: Bank operation code (CRED, DEBT, etc.)
/// - :32A: Value date, currency, amount (format: YYMMDDCURRENCYAMOUNT,)
/// - :50K: Ordering customer (sender) - can be multi-line
/// - :59: Beneficiary customer (receiver) - can be multi-line
/// - :71A: Charges indicator (OUR, BEN, SHA)
/// </summary>
public class MT103Parser : IMT103Parser
{
    private readonly ILogger<MT103Parser> _logger;

    public MT103Parser(ILogger<MT103Parser> logger)
    {
        _logger = logger;
    }

    public SwiftMtxPayment Parse(string mt103Text)
    {
        if (string.IsNullOrWhiteSpace(mt103Text))
        {
            throw new ArgumentException("MT103 text cannot be null or empty", nameof(mt103Text));
        }

        _logger.LogInformation("Parsing MT103 message");

        var mtx = new SwiftMtxPayment
        {
            Status = "RECEIVED",
            ReceivedAt = DateTime.UtcNow
        };

        // Parse :20: Transaction reference
        mtx.TransactionId = ExtractTagValue(mt103Text, ":20:");
        _logger.LogInformation("Parsed :20: TransactionId = {TransactionId}", mtx.TransactionId);

        // Parse :23B: Bank operation code
        mtx.BankOperationCode = ExtractTagValue(mt103Text, ":23B:");
        _logger.LogInformation("Parsed :23B: BankOperationCode = {BankOperationCode}", mtx.BankOperationCode);

        // Parse :32A: Value date, currency, amount
        var field32A = ExtractTagValue(mt103Text, ":32A:");
        if (!string.IsNullOrEmpty(field32A))
        {
            ParseField32A(field32A, mtx);
            _logger.LogInformation("Parsed :32A: ValueDate={ValueDate}, Currency={Currency}, Amount={Amount}",
                mtx.ValueDate, mtx.Currency, mtx.Amount);
        }

        // Parse :50K: Ordering customer (can be multi-line)
        var field50K = ExtractMultiLineTag(mt103Text, ":50K:");
        if (!string.IsNullOrEmpty(field50K))
        {
            mtx.OrderingCustomer = ParseParty(field50K);
            _logger.LogInformation("Parsed :50K: OrderingCustomer = {Name}, Account = {Account}",
                mtx.OrderingCustomer.Name, mtx.OrderingCustomer.Account);
        }

        // Parse :59: Beneficiary customer (can be multi-line)
        var field59 = ExtractMultiLineTag(mt103Text, ":59:");
        if (!string.IsNullOrEmpty(field59))
        {
            mtx.BeneficiaryCustomer = ParseParty(field59);
            _logger.LogInformation("Parsed :59: BeneficiaryCustomer = {Name}, Account = {Account}",
                mtx.BeneficiaryCustomer.Name, mtx.BeneficiaryCustomer.Account);
        }

        // Parse :71A: Charges indicator
        mtx.Charges = ExtractTagValue(mt103Text, ":71A:");
        if (string.IsNullOrEmpty(mtx.Charges))
        {
            mtx.Charges = "OUR"; // Default to sender pays
        }
        _logger.LogInformation("Parsed :71A: Charges = {Charges}", mtx.Charges);

        _logger.LogInformation("MT103 parsing completed successfully");

        return mtx;
    }

    /// <summary>
    /// Extracts value for a single-line tag (e.g., :20:, :71A:)
    /// </summary>
    private string ExtractTagValue(string mt103Text, string tag)
    {
        var pattern = $@"{Regex.Escape(tag)}([^\r\n:]+)";
        var match = Regex.Match(mt103Text, pattern);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    /// <summary>
    /// Extracts value for multi-line tag (e.g., :50K:, :59:)
    /// Handles continuation lines that don't start with a tag
    /// </summary>
    private string ExtractMultiLineTag(string mt103Text, string tag)
    {
        var pattern = $@"{Regex.Escape(tag)}([^\r\n:]+(?:\r?\n[^\r\n:]+)*)";
        var match = Regex.Match(mt103Text, pattern, RegexOptions.Multiline);
        if (!match.Success)
        {
            return string.Empty;
        }

        var value = match.Groups[1].Value;
        // Remove continuation indicators and clean up
        value = Regex.Replace(value, @"\r?\n", " ");
        return value.Trim();
    }

    /// <summary>
    /// Parses :32A: field (Value date, currency, amount)
    /// Format: YYMMDDCURRENCYAMOUNT,
    /// Example: 250925USD1000, → Date: 2025-09-25, Currency: USD, Amount: 1000.00
    /// </summary>
    private void ParseField32A(string field32A, SwiftMtxPayment mtx)
    {
        // Remove whitespace
        field32A = field32A.Replace(" ", "").Replace("\r", "").Replace("\n", "");

        // Extract date (first 6 digits: YYMMDD)
        if (field32A.Length >= 6)
        {
            var year = int.Parse(field32A.Substring(0, 2));
            var month = int.Parse(field32A.Substring(2, 2));
            var day = int.Parse(field32A.Substring(4, 2));

            // Convert YY to YYYY (assume 2000-2099)
            var fullYear = year < 50 ? 2000 + year : 1900 + year;

            mtx.ValueDate = new DateTime(fullYear, month, day);
        }

        // Extract currency (3 letters after date)
        if (field32A.Length >= 9)
        {
            mtx.Currency = field32A.Substring(6, 3);
        }

        // Extract amount (everything after currency, remove comma)
        if (field32A.Length > 9)
        {
            var amountStr = field32A.Substring(9).Replace(",", "");
            if (decimal.TryParse(amountStr, out var amount))
            {
                mtx.Amount = amount;
            }
        }
    }

    /// <summary>
    /// Parses party information from :50K: or :59: fields
    /// Format can be: /ACCOUNT\nNAME or just NAME
    /// </summary>
    private Party ParseParty(string fieldValue)
    {
        var party = new Party();

        // Split by newline or space
        var parts = fieldValue.Split(new[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            // If starts with /, it's an account
            if (trimmed.StartsWith("/"))
            {
                party.Account = trimmed;
            }
            else if (string.IsNullOrEmpty(party.Name))
            {
                // First non-account part is the name
                party.Name = trimmed;
            }
            else
            {
                // Additional parts go to address
                party.Address = string.IsNullOrEmpty(party.Address) 
                    ? trimmed 
                    : $"{party.Address} {trimmed}";
            }
        }

        return party;
    }
}


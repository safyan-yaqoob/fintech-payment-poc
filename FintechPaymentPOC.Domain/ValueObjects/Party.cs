namespace FintechPaymentPOC.Domain.ValueObjects;

/// <summary>
/// Represents a party (customer, bank, etc.) in a payment transaction
/// Used in MTX canonical model to represent ordering customer and beneficiary customer
/// </summary>
public class Party
{
    /// <summary>
    /// Name of the party (e.g., "JOHN DOE", "JANE DOE")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Account identifier (e.g., "/123456789", "/987654321")
    /// Can include account number, IBAN, or other identifiers
    /// </summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>
    /// Additional address information (optional)
    /// </summary>
    public string? Address { get; set; }
}


using FintechPaymentPOC.Domain.ValueObjects;

namespace FintechPaymentPOC.Application.Interfaces;

/// <summary>
/// Parser for SWIFT MT103 messages
/// Converts raw MT103 text into MTX canonical model
/// </summary>
public interface IMT103Parser
{
    /// <summary>
    /// Parses a raw MT103 message string into SwiftMtxPayment
    /// </summary>
    /// <param name="mt103Text">Raw MT103 message text</param>
    /// <returns>Parsed SwiftMtxPayment object</returns>
    SwiftMtxPayment Parse(string mt103Text);
}


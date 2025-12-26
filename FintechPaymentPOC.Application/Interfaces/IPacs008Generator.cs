using FintechPaymentPOC.Domain.ValueObjects;

namespace FintechPaymentPOC.Application.Interfaces;

/// <summary>
/// Generator for ISO 20022 pacs.008 XML messages
/// </summary>
public interface IPacs008Generator
{
    /// <summary>
    /// Generates pacs.008 XML from SwiftMtxPayment
    /// </summary>
    /// <param name="mtx">MTX payment to convert</param>
    /// <returns>pacs.008 XML string</returns>
    string Generate(SwiftMtxPayment mtx);
}


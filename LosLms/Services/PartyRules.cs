using LosLms.Models;

namespace LosLms.Services;

/// <summary>
/// Rules about which parties are actually part of an application.
/// </summary>
public static class PartyRules
{
    /// <summary>
    /// Whether this application has a guarantor. A guarantor counts only once they have been named —
    /// an empty placeholder row does not make one exist.
    /// </summary>
    /// <remarks>
    /// A predicate over an already-loaded list rather than its own query: the Document Checklist,
    /// Reports (RCU) and Eligibility screens all load the party list for other reasons, so a
    /// database round-trip here would re-read what the caller is holding.
    /// </remarks>
    public static bool HasGuarantor(IEnumerable<Party> parties) =>
        parties.Any(p => p.PartyType == "Guarantor" && !string.IsNullOrWhiteSpace(p.FullName));
}

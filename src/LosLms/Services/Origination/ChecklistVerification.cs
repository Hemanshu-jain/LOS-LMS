using LosLms.Models;

namespace LosLms.Services;

/// <summary>
/// Stage 4 treats a PAN or Aadhaar already verified through Digio (PAN check / DigiLocker) as collected, so nobody uploads a scan
/// of something the system has proven. Keyed (party type, checklist document type) -> when it was verified.
/// </summary>
public static class ChecklistVerification
{
    private static readonly Dictionary<string, string> DocumentForSubject = new(StringComparer.Ordinal)
    {
        ["PAN"] = "Pan",
        ["AADHAAR"] = "Aadhaar",
    };

    public static Dictionary<(string PartyType, string DocumentType), DateTime> VerifiedOn(
        IEnumerable<VerificationEvidence> evidence, IEnumerable<Party> parties)
    {
        var typeByPartyId = parties.ToDictionary(p => p.Id, p => p.PartyType);
        var result = new Dictionary<(string, string), DateTime>();

        foreach (var e in evidence.Where(e => e.Verdict == "Verified" && e.PartyId is not null))
        {
            if (DocumentForSubject.TryGetValue(e.Subject, out var doc) && typeByPartyId.TryGetValue(e.PartyId!.Value, out var partyType))
            {
                var key = (partyType, doc);
                if (!result.TryGetValue(key, out var seen) || e.CreatedAt > seen)
                {
                    result[key] = e.CreatedAt;
                }
            }
        }

        return result;
    }
}

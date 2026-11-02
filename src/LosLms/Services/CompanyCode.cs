namespace LosLms.Services;

/// <summary>
/// Generates a short unique company code (3 characters) from a company name. The code becomes the
/// prefix of every application id that company creates (e.g. "KIS" gives KIS-2026-000001), so it must
/// be unique across all companies. The generator keeps the code name-derived where it can, and
/// guarantees uniqueness by varying characters once the natural choice is taken.
/// </summary>
public static class CompanyCode
{
    public const int Length = 3;

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";

    /// <summary>
    /// A 3-character code for <paramref name="name"/> that is not in <paramref name="taken"/>. Prefers
    /// the first letters of the name; falls back to varying the trailing character(s) so a result is
    /// always found. Comparison is case-insensitive; the returned code is upper-case.
    /// </summary>
    public static string Generate(string? name, ISet<string> taken)
    {
        // Normalise the taken set to upper case so clash detection is case-insensitive regardless of how
        // the caller stored existing codes. Every candidate below is already upper case.
        var used = new HashSet<string>(taken.Select(t => t.ToUpperInvariant()));

        var letters = new string((name ?? string.Empty).ToUpperInvariant().Where(IsAsciiLetter).ToArray());

        // Primary candidate: the first three letters of the name, padded with X when the name is shorter.
        var baseCode = (letters.Length >= Length ? letters[..Length] : (letters + "XXX")[..Length]);
        if (!used.Contains(baseCode)) { return baseCode; }

        // Keep the first two characters; vary the third, preferring other letters from the name, then the
        // rest of the alphabet, then digits.
        var head2 = baseCode[..2];
        foreach (var c in OtherLettersOf(letters) + Alphabet + Digits)
        {
            var candidate = head2 + c;
            if (!used.Contains(candidate)) { return candidate; }
        }

        // Keep only the first character; vary the last two over the full alphanumeric range.
        var head1 = baseCode[..1];
        foreach (var a in Alphabet + Digits)
        {
            foreach (var b in Alphabet + Digits)
            {
                var candidate = $"{head1}{a}{b}";
                if (!used.Contains(candidate)) { return candidate; }
            }
        }

        // Exhaustive last resort (effectively never reached — 46,656 codes exist).
        foreach (var a in Alphabet + Digits)
        {
            foreach (var b in Alphabet + Digits)
            {
                foreach (var c in Alphabet + Digits)
                {
                    var candidate = $"{a}{b}{c}";
                    if (!used.Contains(candidate)) { return candidate; }
                }
            }
        }

        throw new InvalidOperationException("No free company code is available.");
    }

    // Distinct letters of the name after the first two, in order, so the varied third character still
    // comes from the name where possible.
    private static string OtherLettersOf(string letters)
    {
        var seen = new HashSet<char>();
        var result = new List<char>();
        foreach (var c in letters.Skip(2))
        {
            if (seen.Add(c)) { result.Add(c); }
        }

        return new string(result.ToArray());
    }

    private static bool IsAsciiLetter(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
}

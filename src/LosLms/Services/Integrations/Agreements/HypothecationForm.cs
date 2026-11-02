namespace LosLms.Services.Agreements;

/// <summary>One line of the form text: its kind (the part before the first colon) and its <c>|</c>-separated parts.</summary>
internal sealed record FormLine(string Kind, string[] Parts)
{
    public string Text => Parts.Length == 0 ? string.Empty : Parts[^1];
    public string Part(int i) => i < Parts.Length ? Parts[i] : string.Empty;
}

/// <summary>A printed page: its body, and the lines that sit in the fixed footer block (below <c>FOOT:</c>), where signature blocks stay put.</summary>
internal sealed record FormPage(int Number, string Flag, int? Printed, IReadOnlyList<FormLine> Body, IReadOnlyList<FormLine> Foot);

/// <summary>Reads <see cref="HypothecationText.Form"/>.</summary>
internal static class HypothecationForm
{
    // Kinds whose text may itself contain a pipe-free label: split into at most this many parts.
    private static readonly Dictionary<string, int> MaxParts = new(StringComparer.Ordinal)
    {
        ["H"] = 2, ["HB"] = 2, ["A"] = 2, ["N"] = 2, ["H2"] = 3,
    };

    // Declared after MaxParts: static initialisers run in textual order and Parse reads it.
    public static IReadOnlyList<FormPage> Pages { get; } = Parse(HypothecationText.Form);

    internal static IReadOnlyList<FormPage> Parse(string text)
    {
        var pages = new List<FormPage>();
        int number = 0, flagStart = 0;
        string flag = string.Empty;
        int? printed = null;
        var body = new List<FormLine>();
        var foot = new List<FormLine>();
        var inFoot = false;

        void Flush()
        {
            if (number > 0) { pages.Add(new FormPage(number, flag, printed, body.ToArray(), foot.ToArray())); }
            body = new List<FormLine>();
            foot = new List<FormLine>();
            inFoot = false;
        }

        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.Length == 0) { continue; }

            if (line.StartsWith("@page", StringComparison.Ordinal))
            {
                Flush();
                var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                number = int.Parse(tokens[1], System.Globalization.CultureInfo.InvariantCulture);
                flag = string.Empty;
                printed = null;
                foreach (var token in tokens.Skip(2))
                {
                    if (token.StartsWith("num=", StringComparison.Ordinal)) { printed = int.Parse(token[4..], System.Globalization.CultureInfo.InvariantCulture); }
                    else { flag = token; }
                }

                flagStart++;
                continue;
            }

            var colon = line.IndexOf(':');
            var kind = line[..colon];
            var rest = line[(colon + 1)..].TrimStart();
            if (kind == "FOOT") { inFoot = true; continue; }

            var parts = MaxParts.TryGetValue(kind, out var max) ? rest.Split('|', max) : rest.Split('|');
            (inFoot ? foot : body).Add(new FormLine(kind, parts));
        }

        Flush();
        return pages;
    }
}

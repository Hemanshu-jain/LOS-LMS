using System.Text.RegularExpressions;
using Xunit;

namespace LosLms.Tests;

/// <summary>
/// "Billing must be perfect": every place that calls Digio must say how the call is billed, in the source, so a call without a meter
/// cannot be added silently. (DigioClient itself also refuses an undeclared call at run time.)
/// </summary>
public class DigioBillingArchitectureTests
{
    private static string AppRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "LosLms.sln")))
        {
            dir = dir.Parent;
        }

        return Path.Combine(dir?.FullName ?? throw new InvalidOperationException("LosLms.sln not found"), "LosLms");
    }

    /// <summary>The text between the parentheses that open at <paramref name="open"/>.</summary>
    private static string Args(string text, int open)
    {
        var depth = 0;
        for (var i = open; i < text.Length; i++)
        {
            if (text[i] == '(') { depth++; }
            else if (text[i] == ')' && --depth == 0) { return text[(open + 1)..i]; }
        }

        return text[(open + 1)..];
    }

    private static IEnumerable<(string File, string Call, string Args)> Calls(string pattern)
    {
        var skip = new[] { Path.Combine("Services", "Integrations", "DigioClient.cs"), "obj", "bin", "Migrations" };
        foreach (var file in Directory.EnumerateFiles(AppRoot(), "*.*", SearchOption.AllDirectories).Where(f => f.EndsWith(".cs") || f.EndsWith(".razor")))
        {
            if (skip.Any(s => file.Contains(s)))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (Match m in Regex.Matches(text, pattern))
            {
                yield return (Path.GetRelativePath(AppRoot(), file), m.Value, Args(text, m.Index + m.Length - 1));
            }
        }
    }

    [Fact]
    public void EveryDigioCall_DeclaresAnApiCodeOrAFreeReason()
    {
        var calls = Calls(@"new DigioCall\(").ToList();
        Assert.NotEmpty(calls); // the scan found the code at all

        var bad = calls.Where(c => !c.Args.Contains("ApiCode:") && !c.Args.Contains("Free:") && !c.Args.Contains("DigioRates.")).Select(c => c.File).ToList();
        Assert.True(bad.Count == 0, "DigioCall without ApiCode/Free in: " + string.Join(", ", bad));
    }

    [Fact]
    public void EveryPostGetOrDownload_ToDigio_DeclaresAnApiCodeOrAFreeReason()
    {
        var calls = Calls(@"\b[dD]igio\.(PostAsync|GetAsync|DownloadAsync)\(").ToList();
        Assert.NotEmpty(calls);

        // A priced code (DigioRates.X) or a trailing free-reason: a string literal or a variable named free.
        var bad = calls.Where(c => !c.Args.Contains("DigioRates.") && !Regex.IsMatch(c.Args.Trim(), @"(,\s*""[^""]+""|,\s*free)\s*$")).Select(c => c.File + ": " + c.Call).ToList();
        Assert.True(bad.Count == 0, "Digio call without a billing declaration in: " + string.Join(", ", bad));
    }
}

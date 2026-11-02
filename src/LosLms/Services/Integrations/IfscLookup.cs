using System.Text.Json;
using System.Text.RegularExpressions;

namespace LosLms.Services;

/// <summary>Bank details resolved from an IFSC code.</summary>
/// <param name="BankName">Always present.</param>
/// <param name="Branch">Null when the result came from the offline prefix table, which is bank-level.</param>
/// <param name="Address">Null when the result came from the offline prefix table.</param>
public sealed record IfscDetails(string BankName, string? Branch, string? Address);

/// <summary>
/// Resolves an IFSC code to a bank, branch and address.
/// </summary>
/// <remarks>
/// This is the ONLY live external call in the build. Everything else that would need a vendor —
/// PAN, Aadhaar, mobile, video KYC, penny drop, NACH registration — stays an honest "not configured"
/// stub, because every one of those needs a contract and a key. Razorpay publishes this one
/// anonymously with no key and no agreement, which is the entire reason it can be real.
///
/// Shared by three screens: Bank &amp; Financial, and the E-Nach and Security-Nach mandate flows.
///
/// Modelled on the PIN-code lookup in CustomerDetails: a local table first, the public endpoint
/// second, manual entry last, and never a hard failure. An officer must always be able to type the
/// details in and save.
/// </remarks>
public static class IfscLookup
{
    /// <summary>Four letters, a zero, then six letters or digits.</summary>
    public static readonly Regex Pattern = new(@"^[A-Z]{4}0[A-Z0-9]{6}$", RegexOptions.Compiled);

    private const string Endpoint = "https://ifsc.razorpay.com/";

    /// <summary>Long enough for a slow mobile connection, short enough not to strand the officer.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(6);

    public static bool IsWellFormed(string? ifsc) =>
        !string.IsNullOrWhiteSpace(ifsc) && Pattern.IsMatch(ifsc.Trim().ToUpperInvariant());

    /// <summary>
    /// Looks the code up against the live directory. Returns null for an unknown code, an unreachable
    /// endpoint, a timeout, or a malformed response — the caller decides what to fall back to, and it
    /// is never an error the officer has to clear.
    /// </summary>
    public static async Task<IfscDetails?> FetchAsync(
        string? ifsc,
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        if (!IsWellFormed(ifsc))
        {
            return null;
        }

        var code = ifsc!.Trim().ToUpperInvariant();

        try
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(Timeout);

            var http = httpClientFactory.CreateClient();
            using var response = await http.GetAsync(Endpoint + code, timeoutSource.Token);

            // 404 is the documented answer for a code that does not exist. Not an error — just a miss.
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeoutSource.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: timeoutSource.Token);

            var bank = ReadString(document.RootElement, "BANK");
            if (string.IsNullOrWhiteSpace(bank))
            {
                return null;
            }

            return new IfscDetails(
                bank,
                ReadString(document.RootElement, "BRANCH"),
                ReadString(document.RootElement, "ADDRESS"));
        }
        catch (Exception)
        {
            // Swallowed on purpose. A directory lookup is a convenience; the officer can always type
            // the bank in and save. Letting this throw would take the whole form down over a flaky
            // network, which is a far worse outcome than an unfilled field.
            return null;
        }
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}

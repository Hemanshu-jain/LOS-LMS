using System.Reflection;
using System.Text;
using LosLms.Licensing;

namespace LosLms.Services;

/// <summary>
/// The vendor's license PUBLIC key (safe to ship) and the initial license baked into this build. The
/// private half never ships — the vendor's license tool signs with it. Verifying here means a client
/// cannot forge a license or push out their own expiry.
/// </summary>
public static class LicenseKeys
{
    private const string PublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEA8YfgNOe6Z5jCs22VjmuL
        ELYc4G8O7KrSz5zfUyM2GFq/I27mc4AlbkwJ1K9F/OCn/pTY7rMQyezsgAGHw5d9
        4gGUMyMeO4GtQieTNtWWynF1eMmFze5PAEtjNelOIBCjRfFVSjAshrNpa9u2dc4j
        ls7nG9LVJJEIxYVRQShmRGfVglg/QHrtlbhNvoSpMC7n2QHKUGxiD+QagXPDW+Jm
        IWgmd6gsW9v13T4OqSqHzx2rIEN2Yc31kGNI57CRzit23uAm30WX3h2YMvHX28/G
        L7KXm3vVhI3dWVQd5y8xraoJwv/t1MAwjB2RTN3Jw3s9fm1faK9ZMu0Fg1u+YedR
        KtvfEm3TX8drWA9mxGJ7f/P6LH144LOY6iuSBOlLw0ifKWjeD+OIEoHeTRBOOGSV
        a9JyE0sknYecFyrziTx9tUxG+1oC3HFTpFLqI/+NPxHinlv8MN+46w7pYEDScSwz
        4HAVCPmkK0LkLCEzgddWYb2zQ2Ok3h7Xw963ed5Jgid3AgMBAAE=
        -----END PUBLIC KEY-----
        """;

    /// <summary>Verifies a license token against the baked public key. False on anything wrong.</summary>
    public static bool TryVerify(string token, out License? license) =>
        LicenseToken.TryVerify(token, PublicKeyPem, out license);

    /// <summary>The license token baked into this build at publish (base64 in assembly metadata), or null.</summary>
    public static string? BakedToken { get; } = ReadBaked();

    private static string? ReadBaked()
    {
        var encoded = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "License")?.Value;

        if (string.IsNullOrEmpty(encoded))
        {
            return null;
        }

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encoded)).Trim();
        }
        catch
        {
            return null;
        }
    }
}

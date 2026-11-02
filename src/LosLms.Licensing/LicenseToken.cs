using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LosLms.Licensing;

/// <summary>
/// Encodes a <see cref="License"/> as a compact signed token — <c>base64url(payload).base64url(signature)</c>
/// — and verifies it. The vendor SIGNS with the RSA private key (in the license tool); the app VERIFIES
/// with the baked public key. A tampered payload (e.g. a pushed-out expiry) fails verification.
/// </summary>
public static class LicenseToken
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = null };

    /// <summary>Signs a license with the vendor's RSA private key (PEM) and returns the token string.</summary>
    public static string Sign(License license, string privateKeyPem)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(license, Json);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return $"{Base64Url(payload)}.{Base64Url(signature)}";
    }

    /// <summary>
    /// Verifies the token against the vendor's public key (PEM) and returns the license. Returns false —
    /// never throws — on a malformed token, a bad signature, or any error.
    /// </summary>
    public static bool TryVerify(string token, string publicKeyPem, out License? license)
    {
        license = null;
        try
        {
            var parts = token.Trim().Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            var payload = FromBase64Url(parts[0]);
            var signature = FromBase64Url(parts[1]);

            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            if (!rsa.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            {
                return false;
            }

            license = JsonSerializer.Deserialize<License>(payload, Json);
            return license is not null;
        }
        catch
        {
            return false;
        }
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string s)
    {
        var b = s.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(b.PadRight(b.Length + (4 - b.Length % 4) % 4, '='));
    }
}

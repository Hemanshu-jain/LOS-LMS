using System.Security.Cryptography;

namespace LosLms.Services;

/// <summary>
/// Verifies that a downloaded update was signed by the vendor before it is ever staged for the watchdog
/// to apply.
/// </summary>
/// <remarks>
/// Only the PUBLIC half of the vendor's RSA key is baked here — safe to ship and decompile. The PRIVATE
/// half never leaves the vendor's machine (<c>update-signing-private.pem</c>, gitignored) and is what
/// <c>publish.ps1</c> uses to sign each release zip. That asymmetry is the whole point: a client can
/// PROVE an update really came from the vendor and was not altered — in transit, or by anyone who gains
/// write access to the public GitHub release — but no one who only has the shipped binary can forge one.
///
/// The GitHub API reads that discover releases are unauthenticated (a public repo), so without this a
/// swapped release asset would be applied with no questions asked. Rotating the key means replacing the
/// PEM below and re-signing future releases with the new private key.
/// </remarks>
public static class UpdateSigning
{
    private const string PublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEAw5cLzrxtZf3IqVIMDLg3
        cN27YbccQ//SDn4c0txWi7oX4uSHAMl+LWpi5rIM4k71fnQqJ9f27H0g5CcYtsDg
        zMVLLhltfaeN9FGbQKUdKekFQAAEiEQ7ZfFa1jruxKqP1Oc6Rr4bRpTrBxSYAQ+3
        JeRSvR9vr4hN5mibJJJlRwMMe77DAGJR+ItMWVzYfViYUhDro4ID3CyJ7cSNTJ3X
        Ts3Ob2JsJTgPeusw4xgB8pp1HOSfY1/VoIPA/+PRB/1mza0D8XW1qDw0Jq1Kw8PF
        RZVOZpvULn791KyUqE/iS+Zi61ujGsedAPmqdq+pDD1kUfUUKObv230CylPSLVJb
        A4Bd1skZwXpNIKnLMg/9s3Ddv99cV3Ddx0MDvtGsBrNbOcpgEUU/WcOy+y2ehTKZ
        B66Gqh1Ocu9ck3pfjIK/VlvNWxSW4VzNe+u92WIlR4FWM3zxTjm/WM+Qdspkt0ie
        N1edSZz2/7D+W7lkGzNXLNOoIgV+nz5e59l38s5+A3BtAgMBAAE=
        -----END PUBLIC KEY-----
        """;

    /// <summary>
    /// True only when <paramref name="signature"/> is a valid vendor signature over the exact bytes of
    /// the file at <paramref name="filePath"/>. Any tampering, a wrong key, or a malformed signature
    /// returns false rather than throwing.
    /// </summary>
    public static bool VerifyFile(string filePath, byte[] signature)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(PublicKeyPem);
            using var stream = File.OpenRead(filePath);
            return rsa.VerifyData(stream, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }
}

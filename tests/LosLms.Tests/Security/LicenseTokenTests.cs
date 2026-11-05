using System.Security.Cryptography;
using LosLms.Licensing;
using Xunit;

namespace LosLms.Tests;

/// <summary>
/// The licence is the subscription gate, so its sign/verify must be exact: a real signature verifies and
/// round-trips; any tampering (a pushed-out expiry) or a wrong key is rejected.
/// </summary>
public sealed class LicenseTokenTests
{
    private static (string PrivatePem, string PublicPem) NewKeypair()
    {
        using var rsa = RSA.Create(3072);
        return (rsa.ExportPkcs8PrivateKeyPem(), rsa.ExportSubjectPublicKeyInfoPem());
    }

    private static License Sample(DateTimeOffset expires) => new()
    {
        Client = "ABC Finance",
        Host = "abc.bhodhix.com",
        TunnelId = "tunnel-123",
        IssuedUtc = DateTimeOffset.UtcNow,
        ExpiresUtc = expires,
    };

    [Fact]
    public void SignedLicense_Verifies_AndRoundTrips()
    {
        var (priv, pub) = NewKeypair();
        var expires = DateTimeOffset.UtcNow.AddMonths(12);

        var token = LicenseToken.Sign(Sample(expires), priv);

        Assert.True(LicenseToken.TryVerify(token, pub, out var lic));
        Assert.NotNull(lic);
        Assert.Equal("ABC Finance", lic!.Client);
        Assert.Equal("abc.bhodhix.com", lic.Host);
        Assert.Equal(expires.ToUnixTimeSeconds(), lic.ExpiresUtc.ToUnixTimeSeconds());
    }

    [Fact]
    public void TamperedPayload_IsRejected()
    {
        var (priv, pub) = NewKeypair();
        var token = LicenseToken.Sign(Sample(DateTimeOffset.UtcNow.AddMonths(1)), priv);

        // Flip a character in the payload half — the signature no longer matches.
        var parts = token.Split('.');
        var body = parts[0].ToCharArray();
        body[5] = body[5] == 'A' ? 'B' : 'A';
        var tampered = new string(body) + "." + parts[1];

        Assert.False(LicenseToken.TryVerify(tampered, pub, out _));
    }

    [Fact]
    public void WrongPublicKey_IsRejected()
    {
        var (priv, _) = NewKeypair();
        var (_, otherPub) = NewKeypair();
        var token = LicenseToken.Sign(Sample(DateTimeOffset.UtcNow.AddMonths(1)), priv);

        Assert.False(LicenseToken.TryVerify(token, otherPub, out _));
    }

    [Fact]
    public void Garbage_IsRejected()
    {
        var (_, pub) = NewKeypair();
        Assert.False(LicenseToken.TryVerify("not-a-token", pub, out _));
        Assert.False(LicenseToken.TryVerify("", pub, out _));
    }
}

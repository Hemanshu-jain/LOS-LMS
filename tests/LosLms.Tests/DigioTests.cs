using System.Security.Cryptography;
using System.Text;
using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

/// <summary>Digio webhook trust: the HMAC checksum. What a verified webhook may do is in <see cref="DigioWebhookTests"/>.</summary>
public sealed class DigioTests
{
    [Fact]
    public void Checksum_AcceptsDigioHmac_RejectsAnythingElse()
    {
        const string body = "{\"event\":\"doc.signed\"}";
        var hex = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes("s3cret"), Encoding.UTF8.GetBytes(body)));

        Assert.True(DigioClient.ChecksumMatches("s3cret", body, hex.ToLowerInvariant()));
        Assert.True(DigioClient.ChecksumMatches("s3cret", body, hex)); // case-insensitive hex
        Assert.False(DigioClient.ChecksumMatches("wrong", body, hex));
        Assert.False(DigioClient.ChecksumMatches("s3cret", body + " ", hex)); // tampered body
        Assert.False(DigioClient.ChecksumMatches("s3cret", body, null));
    }
}

using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

public sealed class FileSignatureTests
{
    private static readonly byte[] Pdf = { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };
    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 };
    private static readonly byte[] Webp = { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 };

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".webp")]
    public void RealContent_MatchesItsExtension(string extension)
    {
        var content = extension switch
        {
            ".pdf" => Pdf,
            ".jpg" or ".jpeg" => Jpeg,
            ".png" => Png,
            ".webp" => Webp,
            _ => throw new ArgumentOutOfRangeException(nameof(extension)),
        };
        Assert.True(FileSignature.Matches(extension, content));
    }

    [Fact]
    public void RenamedExecutable_IsRejected()
    {
        var mzExe = new byte[] { 0x4D, 0x5A, 0x90, 0x00 }; // "MZ" — a Windows PE, renamed to .pdf
        Assert.False(FileSignature.Matches(".pdf", mzExe));
    }

    [Fact]
    public void ContentOfWrongType_IsRejected()
    {
        Assert.False(FileSignature.Matches(".png", Pdf));   // a real PDF claiming to be a PNG
        Assert.False(FileSignature.Matches(".pdf", Png));   // a real PNG claiming to be a PDF
    }

    [Fact]
    public void TooShortOrEmpty_IsRejected()
    {
        Assert.False(FileSignature.Matches(".png", new byte[] { 0x89, 0x50 }));
        Assert.False(FileSignature.Matches(".pdf", Array.Empty<byte>()));
    }
}

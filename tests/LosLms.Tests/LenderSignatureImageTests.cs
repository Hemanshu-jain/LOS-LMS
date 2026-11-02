using LosLms.Services;
using QuestPDF.Fluent;
using Xunit;

namespace LosLms.Tests;

public sealed class LenderSignatureImageTests
{
    /// <summary>A solid-colour PNG of the given size, made with the same PDF engine (so the test needs no image library).</summary>
    private static byte[] Png(int width, int height)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        return Document.Create(d => d.Page(p =>
        {
            p.Size(new QuestPDF.Helpers.PageSize(width, height));
            p.Margin(0);
            p.Content().Background("#224488");
        })).GenerateImages(new QuestPDF.Infrastructure.ImageGenerationSettings { ImageFormat = QuestPDF.Infrastructure.ImageFormat.Png, RasterDpi = 72 }).First();
    }

    [Fact]
    public void BigImage_IsScaledDownToAtMost800Wide_KeepingTheShape()
    {
        var big = Png(3200, 1200);

        var result = LenderSignatureImage.Normalize("sig.png", big);

        var (w, h) = LenderSignatureImage.PngSize(result);
        Assert.Equal(800, w);
        Assert.InRange(h, 299, 301);
        Assert.True(result.Length < 1024 * 1024, "stored well under 1 MB");
    }

    [Fact]
    public void SmallImage_IsNeverEnlarged()
    {
        var (w, h) = LenderSignatureImage.PngSize(LenderSignatureImage.Normalize("sig.png", Png(400, 150)));

        Assert.Equal(400, w);
        Assert.InRange(h, 149, 151);
    }

    [Fact]
    public void Jpeg_IsAcceptedAndComesOutAsASmallPng()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var jpeg = Document.Create(d => d.Page(p =>
        {
            p.Size(new QuestPDF.Helpers.PageSize(2000, 700));
            p.Margin(0);
            p.Content().Background("#aa3322");
        })).GenerateImages(new QuestPDF.Infrastructure.ImageGenerationSettings { ImageFormat = QuestPDF.Infrastructure.ImageFormat.Jpeg, RasterDpi = 72 }).First();

        var result = LenderSignatureImage.Normalize("scan.JPG", jpeg);

        var (w, h) = LenderSignatureImage.PngSize(result);
        Assert.Equal(800, w);
        Assert.InRange(h, 279, 281);
    }

    [Theory]
    [InlineData("sig.gif")]
    [InlineData("sig.exe")]
    public void WrongTypes_AreRefusedWithAClearMessage(string name)
    {
        var ex = Assert.Throws<InvalidDataException>(() => LenderSignatureImage.Normalize(name, Png(100, 40)));

        Assert.Contains("PNG or JPG", ex.Message);
    }

    [Fact]
    public void RenamedFile_IsRefused() =>
        Assert.Throws<InvalidDataException>(() => LenderSignatureImage.Normalize("sig.png", new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));

    [Fact]
    public void OverTenMegabytes_IsRefused()
    {
        var huge = new byte[LenderSignatureImage.MaxBytes + 1];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(huge, 0);

        var ex = Assert.Throws<InvalidDataException>(() => LenderSignatureImage.Normalize("sig.png", huge));

        Assert.Contains("10 MB", ex.Message);
    }

    [Fact]
    public void JpegSize_ReadsTheFrameHeader()
    {
        // SOI, then an SOF0 segment: length 17, precision 8, height 0x0123, width 0x0345
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x01, 0x23, 0x03, 0x45, 0x03, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        Assert.Equal((0x345, 0x123), LenderSignatureImage.JpegSize(jpeg));
    }
}

using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace LosLms.Services;

/// <summary>
/// The lender's signature image (stamped on every loan agreement; the lender is not an e-Sign signer). Accepts a PNG or JPG up to
/// <see cref="MaxBytes"/> and stores it as a PNG no wider than <see cref="MaxWidth"/> pixels, so a phone photo or a scan never has to be
/// shrunk by hand first.
/// </summary>
public static class LenderSignatureImage
{
    public const int MaxBytes = 10 * 1024 * 1024;
    public const int MaxWidth = 800;
    private const int MaxPixels = 60_000_000;

    public static string KeyFor(int companyId) => $"App_Data/lender-signature/{companyId}.png";

    /// <summary>
    /// Checks the upload and returns it as a PNG at most <see cref="MaxWidth"/> pixels wide (never enlarged). The message of the
    /// <see cref="InvalidDataException"/> is meant for the user.
    /// </summary>
    public static byte[] Normalize(string fileName, byte[] bytes)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg") || !FileSignature.Matches(ext, bytes))
        {
            throw new InvalidDataException("That is not a PNG or JPG image.");
        }

        if (bytes.Length > MaxBytes)
        {
            throw new InvalidDataException("The image is larger than 10 MB. Please choose a smaller file.");
        }

        var (width, height) = ext == ".png" ? PngSize(bytes) : JpegSize(bytes);
        if (width <= 0 || height <= 0 || (long)width * height > MaxPixels)
        {
            throw new InvalidDataException("The image could not be read or is too large in pixels. Try a PNG or JPG of the signature alone.");
        }

        var outWidth = Math.Min(width, MaxWidth);
        var outHeight = Math.Max(1, (int)Math.Round(height * (double)outWidth / width));

        try
        {
            // The PDF engine already ships an image decoder: draw the picture onto a page exactly outWidth x outHeight points and rasterise it
            // at 72 dpi, which gives a PNG of exactly that many pixels (transparency kept).
            var png = Document.Create(document => document.Page(page =>
            {
                page.Size(new QuestPDF.Helpers.PageSize(outWidth, outHeight));
                page.Margin(0);
                page.Content().Image(bytes).FitArea();
            })).GenerateImages(new ImageGenerationSettings { ImageFormat = ImageFormat.Png, RasterDpi = 72 }).First();
            return png;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new InvalidDataException("The image could not be processed. Try saving it again as a PNG or JPG.", ex);
        }
    }

    /// <summary>Normalises and stores the image, replacing any earlier one. Returns the stored key.</summary>
    public static async Task<string> SaveAsync(LosDbContext db, IFileStore files, int companyId, string fileName, byte[] bytes)
    {
        var png = Normalize(fileName, bytes);
        var key = KeyFor(companyId);
        await files.SaveAsync(key, png);

        var row = await db.DigioSettings.FirstOrDefaultAsync(x => x.CompanyId == companyId);
        if (row is null)
        {
            row = new DigioSettings { CompanyId = companyId };
            db.DigioSettings.Add(row);
        }

        row.LenderSignaturePath = key;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return key;
    }

    public static async Task RemoveAsync(LosDbContext db, int companyId)
    {
        var row = await db.DigioSettings.FirstOrDefaultAsync(x => x.CompanyId == companyId);
        if (row?.LenderSignaturePath is not { Length: > 0 }) { return; }

        // The file store has no delete; clearing the reference is what stops the agreement using it (the next save overwrites the file).
        row.LenderSignaturePath = null;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    /// <summary>The stored signature, or null when none is saved or the file is gone.</summary>
    public static async Task<byte[]?> LoadAsync(LosDbContext db, IFileStore files, int companyId)
    {
        var key = await db.DigioSettings.AsNoTracking().Where(x => x.CompanyId == companyId).Select(x => x.LenderSignaturePath).FirstOrDefaultAsync();
        return string.IsNullOrWhiteSpace(key) ? null : await files.ReadAllBytesAsync(key);
    }

    /// <summary>Width and height from the PNG header (IHDR); (0, 0) when it is not a valid header.</summary>
    internal static (int Width, int Height) PngSize(byte[] b) =>
        b.Length >= 24 ? (ReadInt32(b, 16), ReadInt32(b, 20)) : (0, 0);

    /// <summary>Width and height from the first JPEG start-of-frame marker; (0, 0) when none is found.</summary>
    internal static (int Width, int Height) JpegSize(byte[] b)
    {
        var i = 2;
        while (i + 9 < b.Length)
        {
            if (b[i] != 0xFF) { i++; continue; }

            var marker = b[i + 1];
            if (marker is 0xD8 or 0x01 or (>= 0xD0 and <= 0xD7)) { i += 2; continue; }

            var length = (b[i + 2] << 8) | b[i + 3];
            if (marker is >= 0xC0 and <= 0xCF and not (0xC4 or 0xC8 or 0xCC))
            {
                return ((b[i + 7] << 8) | b[i + 8], (b[i + 5] << 8) | b[i + 6]);
            }

            i += 2 + Math.Max(length, 2);
        }

        return (0, 0);
    }

    private static int ReadInt32(byte[] b, int at) => (b[at] << 24) | (b[at + 1] << 16) | (b[at + 2] << 8) | b[at + 3];
}

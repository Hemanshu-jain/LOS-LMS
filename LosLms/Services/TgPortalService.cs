using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// The officer's record of checking a vehicle on the Telangana Transport portal. The portal has a CAPTCHA, so nothing here
/// automates or scrapes it: the app only links to it and keeps what the officer says they did (tick, date, who, optional screenshot).
/// </summary>
public static class TgPortalService
{
    public const string Url = "https://tgtransport.net/TGCFSTONLINE/Reports/VehicleRegistrationSearch.aspx";

    private const int MaxBytes = 5 * 1024 * 1024;

    /// <summary>Ticks or unticks "verified on the TG portal". Creates the security row when the page has not saved one yet.</summary>
    public static async Task MarkAsync(LosDbContext db, string applicationId, bool verified, string? userId, string? userName, DateTime? nowUtc = null)
    {
        var row = await RowAsync(db, applicationId);
        row.TgPortalVerified = verified;
        row.TgPortalVerifiedAt = verified ? nowUtc ?? DateTime.UtcNow : null;
        row.TgPortalVerifiedByUserId = verified ? userId : null;
        row.TgPortalVerifiedBy = verified ? Cut(userName, 100) : null;
        await db.SaveChangesAsync();
    }

    /// <summary>Keeps the officer's screenshot (PDF, JPG, PNG or WebP up to 5 MB) in the file store. Returns the file key, or null with a reason.</summary>
    public static async Task<(string? Key, string? Error)> AttachAsync(LosDbContext db, IFileStore files, string applicationId, string fileName, byte[] bytes)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (bytes.Length is 0 or > MaxBytes)
        {
            return (null, "The file must be under 5 MB.");
        }

        if (ext is not (".pdf" or ".jpg" or ".jpeg" or ".png" or ".webp") || !FileSignature.Matches(ext, bytes))
        {
            return (null, "That is not a PDF, JPG, PNG or WebP file.");
        }

        var row = await RowAsync(db, applicationId);
        var key = FileKey.Normalize(await files.SaveUploadAsync(applicationId, "rcportal", $"tg-{Guid.NewGuid():N}{ext}", bytes));
        row.TgPortalScreenshotPath = key;
        await db.SaveChangesAsync();
        return (key, null);
    }

    private static async Task<SecurityDetail> RowAsync(LosDbContext db, string applicationId)
    {
        var row = await db.SecurityDetails.FirstOrDefaultAsync(s => s.ApplicationId == applicationId);
        if (row is null)
        {
            row = new SecurityDetail { ApplicationId = applicationId };
            db.SecurityDetails.Add(row);
        }

        return row;
    }

    private static string? Cut(string? s, int max) => s is { Length: > 0 } ? (s.Length > max ? s[..max] : s) : null;
}

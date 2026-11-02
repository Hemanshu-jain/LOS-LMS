using System.Text;
using LosLms.Models;
using QRCoder;

namespace LosLms.Services;

/// <summary>
/// Draws the QR code a customer scans to pay one bill. The code carries the same <c>upi://pay</c> link the
/// "Pay with UPI" button used to open (payee, amount and the bill reference), so any UPI app fills those in.
/// </summary>
/// <remarks>
/// The link alone does nothing on a PC: no program there handles <c>upi://</c>. A QR scanned from a phone works
/// everywhere. Rendered as an SVG inside a data: URI so it needs no file, no network call and no script, and fits
/// the app's self-only Content-Security-Policy (<c>img-src</c> allows <c>data:</c>).
/// </remarks>
public static class UpiQr
{
    /// <summary>The QR for one bill as an <c>&lt;img src&gt;</c> value, or null when no UPI id is set.</summary>
    public static string? DataUri(PaymentDetails details, ApiInvoice invoice) =>
        ApiBillingService.UpiLink(details, invoice) is { } link ? DataUri(link) : null;

    /// <summary>The QR for any payload as an SVG data: URI.</summary>
    public static string DataUri(string payload)
    {
        // Level M (about 15% of the code can be damaged and still scan) keeps the code small enough to read from a
        // laptop screen at arm's length, which is how it will be scanned.
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var svg = new SvgQRCode(data).GetGraphic(
            pixelsPerModule: 8, darkColorHex: "#000000", lightColorHex: "#FFFFFF", drawQuietZones: true,
            sizingMode: SvgQRCode.SizingMode.ViewBoxAttribute);

        return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
    }
}

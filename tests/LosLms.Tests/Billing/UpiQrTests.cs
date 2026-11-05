using System.Text;
using LosLms.Models;
using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

/// <summary>The QR a customer scans to pay a bill: drawn from the bill's own UPI link, never without a UPI id.</summary>
public sealed class UpiQrTests
{
    private static readonly PaymentDetails Payee = new() { UpiId = "payee@bank", PayeeName = "Payee Name" };

    private static ApiInvoice Bill(decimal subtotal, decimal gst) =>
        new() { CompanyId = 4, PeriodStart = new DateOnly(2026, 10, 1), Subtotal = subtotal, Gst = gst };

    private static string Svg(string dataUri)
    {
        const string prefix = "data:image/svg+xml;base64,";
        Assert.StartsWith(prefix, dataUri);
        return Encoding.UTF8.GetString(Convert.FromBase64String(dataUri[prefix.Length..]));
    }

    [Fact]
    public void NoUpiId_MeansNoQr() =>
        Assert.Null(UpiQr.DataUri(new PaymentDetails { PayeeName = "Payee Name" }, Bill(21.10m, 3.80m)));

    [Fact]
    public void ABillWithAUpiId_GetsAnSvgImage_ThatScalesWithItsBox()
    {
        var svg = Svg(UpiQr.DataUri(Payee, Bill(21.10m, 3.80m))!);

        Assert.Contains("<svg", svg);
        Assert.Contains("viewBox", svg);
        Assert.Contains("#000000", svg);
    }

    [Fact]
    public void TheCode_FollowsTheBill_SoTheAmountCannotBeStale()
    {
        var small = UpiQr.DataUri(Payee, Bill(21.10m, 3.80m));
        var large = UpiQr.DataUri(Payee, Bill(2110.00m, 380.00m));

        Assert.NotEqual(small, large);
        Assert.Equal(small, UpiQr.DataUri(Payee, Bill(21.10m, 3.80m))); // same bill, same code
    }

    [Fact]
    public void ItEncodesTheSameLinkTheUpiButtonUsed()
    {
        var invoice = Bill(21.10m, 3.80m);
        var link = ApiBillingService.UpiLink(Payee, invoice)!;

        Assert.Equal(UpiQr.DataUri(link), UpiQr.DataUri(Payee, invoice));
        Assert.Contains("am=24.90", link);
        Assert.Contains("tn=LMS-4-202610", link);
    }
}

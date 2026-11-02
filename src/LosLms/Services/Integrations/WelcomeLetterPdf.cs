using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LosLms.Services;

/// <summary>
/// Renders the borrower welcome letter as a PDF. Real document, no external dependency — same QuestPDF
/// pattern as <see cref="CamPdf"/>.
/// </summary>
public static class WelcomeLetterPdf
{
    private static readonly CultureInfo IndianCulture = CultureInfo.GetCultureInfo("en-IN");

    public static string FileNameFor(string applicationId) => $"Welcome-{applicationId}.pdf";

    /// <param name="emi">The month-1 instalment from the adjusted schedule (first override or standard).</param>
    public static byte[] Build(
        string companyName,
        string applicationId,
        string? customerName,
        DateOnly agreementDate,
        decimal sanctionedAmount,
        decimal emi,
        int tenureMonths)
    {
        var company = string.IsNullOrWhiteSpace(companyName) ? "your lender" : companyName.Trim();

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(11).FontColor("#1a1f29"));

                page.Header().Column(header =>
                {
                    header.Item().Text($"Welcome to {company}").FontSize(18).Bold();
                    header.Item().PaddingTop(6).LineHorizontal(1).LineColor("#1a1f29");
                });

                page.Content().PaddingVertical(16).Column(content =>
                {
                    content.Spacing(10);

                    content.Item().Text($"Date: {agreementDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)}");
                    content.Item().Text($"Dear {Display(customerName)},");
                    content.Item().Text(
                        $"Thank you for choosing {company}. We are pleased to confirm the sanction of your loan. "
                        + "The key terms of your facility are set out below.");

                    content.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                        });

                        AddRow(table, "Application", applicationId);
                        AddRow(table, "Agreement date", agreementDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
                        AddRow(table, "Loan amount", Money(sanctionedAmount));
                        AddRow(table, "EMI", Money(emi));
                        AddRow(table, "Tenure", tenureMonths > 0 ? $"{tenureMonths} months" : "—");
                    });

                    content.Item().PaddingTop(10).Text(
                        "We look forward to a long and rewarding association. Welcome aboard.");
                    content.Item().PaddingTop(14).Text($"Warm regards,\n{company}");
                });

                page.Footer().AlignCenter().Text(footer =>
                {
                    footer.Span("Generated ").FontSize(8).FontColor("#93999a");
                    footer.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
                        .FontSize(8).FontColor("#93999a");
                });
            });
        }).GeneratePdf();
    }

    private static void AddRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Element(Cell).Text(label).SemiBold();
        table.Cell().Element(Cell).Text(value);
    }

    private static IContainer Cell(IContainer container) =>
        container.BorderBottom(1).BorderColor("#eceef1").PaddingVertical(5);

    private static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "Customer" : value;

    private static string Money(decimal amount) =>
        "₹" + Math.Round(amount, MidpointRounding.AwayFromZero).ToString("N0", IndianCulture);
}

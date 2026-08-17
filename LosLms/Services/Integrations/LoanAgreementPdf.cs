using System.Globalization;
using LosLms.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LosLms.Services;

/// <summary>
/// Renders a standard loan agreement as a PDF: parties, sanctioned terms and the security offered.
/// Real document via QuestPDF — the same pattern as <see cref="CamPdf"/>. Dispatching it for
/// e-signature is a separate, stubbed step (<see cref="EsignService"/>).
/// </summary>
public static class LoanAgreementPdf
{
    private static readonly CultureInfo IndianCulture = CultureInfo.GetCultureInfo("en-IN");

    public static string FileNameFor(string applicationId) => $"Agreement-{applicationId}.pdf";

    /// <param name="emi">The month-1 instalment from the adjusted schedule.</param>
    public static byte[] Build(
        string companyName,
        Application application,
        ApprovalDecision decision,
        decimal emi,
        IReadOnlyList<Party> parties,
        SecurityDetail? security)
    {
        var lender = string.IsNullOrWhiteSpace(companyName) ? "the Lender" : companyName.Trim();

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor("#1a1f29"));

                page.Header().Column(header =>
                {
                    header.Item().Text("LOAN AGREEMENT").FontSize(16).Bold();
                    header.Item().PaddingTop(2).Text($"Application {application.Id}").FontSize(9).FontColor("#5b6472");
                    header.Item().PaddingTop(6).LineHorizontal(1).LineColor("#1a1f29");
                });

                page.Content().PaddingVertical(14).Column(content =>
                {
                    content.Spacing(12);

                    content.Item().Text(text =>
                    {
                        text.Span("This agreement is made between ");
                        text.Span(lender).SemiBold();
                        text.Span(" (\"the Lender\") and the borrower(s) named below (\"the Borrower\") "
                            + "on the terms set out in this document.");
                    });

                    // ---- Parties ----
                    content.Item().Text("1. Parties").FontSize(11).Bold();
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Role").Bold();
                            header.Cell().Element(HeaderCell).Text("Name").Bold();
                        });

                        if (parties.Count == 0)
                        {
                            table.Cell().Element(BodyCell).Text("Applicant");
                            table.Cell().Element(BodyCell).Text(Display(application.CustomerName));
                        }
                        else
                        {
                            foreach (var party in parties)
                            {
                                table.Cell().Element(BodyCell).Text(RoleLabel(party.PartyType));
                                table.Cell().Element(BodyCell).Text(Display(party.FullName));
                            }
                        }
                    });

                    // ---- Sanctioned terms ----
                    content.Item().Text("2. Sanctioned terms").FontSize(11).Bold();
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                        });

                        AddRow(table, "Loan product", Display(application.LoanProduct));
                        AddRow(table, "Sanctioned amount", Money(decision.SanctionedAmount));
                        AddRow(table, "Rate of interest", decision.SanctionedRoi is { } roi ? $"{roi.ToString("0.##", CultureInfo.InvariantCulture)}% p.a." : "—");
                        AddRow(table, "Tenure", decision.SanctionedTenure is { } t and > 0 ? $"{t} months" : "—");
                        AddRow(table, "EMI", Money(emi));
                    });

                    // ---- Security ----
                    content.Item().Text("3. Security").FontSize(11).Bold();
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                        });

                        if (security is null)
                        {
                            table.Cell().Element(BodyCell).Text("Security");
                            table.Cell().Element(BodyCell).Text("—");
                        }
                        else if (string.Equals(security.AssetType, "Property", StringComparison.OrdinalIgnoreCase))
                        {
                            AddRow(table, "Type", "Immovable property");
                            AddRow(table, "Property type", Display(security.PropertyType));
                            AddRow(table, "Address", Display(security.PropertyAddress));
                        }
                        else
                        {
                            AddRow(table, "Type", "Hypothecation of vehicle");
                            AddRow(table, "Make / model", Display(security.MakeModel));
                            AddRow(table, "Mfg. year", Display(security.MfgYear));
                            AddRow(table, "Registration no.", Display(security.RegNo));
                        }
                    });

                    content.Item().PaddingTop(6).Text(
                        "The Borrower agrees to repay the loan in equated monthly instalments as scheduled, "
                        + "and to the standard terms and conditions of the Lender which form part of this agreement.")
                        .FontSize(9).FontColor("#3a4150");

                    // ---- Signatures ----
                    content.Item().PaddingTop(24).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("_______________________");
                            col.Item().Text("For the Lender").FontSize(9).FontColor("#5b6472");
                        });
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("_______________________");
                            col.Item().Text("Borrower").FontSize(9).FontColor("#5b6472");
                        });
                    });
                });

                page.Footer().AlignCenter().Text(footer =>
                {
                    footer.Span("Generated ").FontSize(8).FontColor("#93999a");
                    footer.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
                        .FontSize(8).FontColor("#93999a");
                    footer.Span("  ·  page ").FontSize(8).FontColor("#93999a");
                    footer.CurrentPageNumber().FontSize(8).FontColor("#93999a");
                    footer.Span(" / ").FontSize(8).FontColor("#93999a");
                    footer.TotalPages().FontSize(8).FontColor("#93999a");
                });
            });
        }).GeneratePdf();
    }

    private static void AddRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Element(BodyCell).Text(label).SemiBold();
        table.Cell().Element(BodyCell).Text(value);
    }

    private static string RoleLabel(string partyType) => partyType switch
    {
        "CoApplicant" => "Co-Applicant",
        "Guarantor" => "Guarantor",
        _ => "Applicant",
    };

    private static IContainer HeaderCell(IContainer container) =>
        container.Background("#f2f2f2").BorderBottom(1).BorderColor("#d4d4d4").Padding(4);

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1).BorderColor("#eceef1").Padding(4);

    private static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static string Money(decimal? amount) =>
        "₹" + Math.Round(amount ?? 0m, MidpointRounding.AwayFromZero).ToString("N0", IndianCulture);
}

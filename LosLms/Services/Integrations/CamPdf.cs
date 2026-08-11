using System.Globalization;
using LosLms.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LosLms.Services;

/// <summary>
/// Renders the Credit Appraisal Memo as a PDF.
/// </summary>
/// <remarks>
/// One generator, two callers: the CAM tab on Bank &amp; Financial and the "View CAM.pdf" button on
/// the shared Application Summary Rail. Neither should ever be reachable unless
/// <see cref="CamCostBreakdown.LastRecalculatedAt"/> is set — every figure below reads the
/// <c>Applied</c> columns, so a CAM that was never recalculated would render as zeros.
/// </remarks>
public static class CamPdf
{
    private static readonly CultureInfo IndianCulture = CultureInfo.GetCultureInfo("en-IN");

    public static string FileNameFor(Application application) => $"CAM-{application.Id}.pdf";

    public static byte[] Build(Application application, CamCostBreakdown cam)
    {
        var rate = application.Roi ?? 0m;
        var months = application.Tenure ?? 0;

        var emi = LoanMath.Emi(application.LoanAmount, rate, months);
        var schedule = LoanMath.Schedule(application.LoanAmount, rate, months);

        var onRoad = (cam.AppliedExShowroomCost ?? 0m)
            + (cam.AppliedBodyAccessories ?? 0m)
            + (cam.AppliedInsuranceRegistration ?? 0m);

        var ltv = onRoad > 0 ? application.LoanAmount / onRoad * 100m : (decimal?)null;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(t => t.FontSize(9).FontColor("#1a1f29"));

                page.Header().Column(header =>
                {
                    header.Item().Text("CREDIT APPRAISAL MEMO").FontSize(15).Bold();
                    header.Item().PaddingTop(2).Text($"Application {application.Id}").FontSize(10).FontColor("#5b6472");
                    header.Item().PaddingTop(6).Text(
                        $"Customer: {Display(application.CustomerName)}    "
                        + $"Branch: {Display(application.Branch)}    "
                        + $"Product: {Display(application.LoanProduct)}    "
                        + $"Scheme: {Display(application.Scheme)}")
                        .FontSize(8.5f).FontColor("#5b6472");
                    header.Item().PaddingTop(6).LineHorizontal(1).LineColor("#1a1f29");
                });

                page.Content().PaddingVertical(10).Column(content =>
                {
                    content.Item().Text("Cost breakdown").FontSize(10).Bold();
                    content.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                        });

                        AddCostRow(table, "Ex-showroom cost", Money(cam.AppliedExShowroomCost));
                        AddCostRow(table, "Body / accessories", Money(cam.AppliedBodyAccessories));
                        AddCostRow(table, "Insurance & registration", Money(cam.AppliedInsuranceRegistration));
                        AddCostRow(table, "On-road cost", Money(onRoad));
                        AddCostRow(table, "Margin / down payment", Money(cam.AppliedMargin));
                        AddCostRow(table, "Processing fee", Money(application.ProcessingFee));
                        AddCostRow(table, "Loan amount", Money(application.LoanAmount));
                        AddCostRow(table, "LTV on on-road cost", ltv.HasValue ? Percent(ltv.Value) : "—");
                    });

                    content.Item().PaddingTop(14).Text("Sanction summary").FontSize(10).Bold();
                    content.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        AddTile(table, "EMI", Money(emi));
                        AddTile(table, "ROI", rate > 0 ? Percent(rate) : "—");
                        AddTile(table, "Term", months > 0 ? $"{months} mo" : "—");
                        AddTile(table, "LTV", ltv.HasValue ? Percent(ltv.Value) : "—");
                    });

                    content.Item().PaddingTop(14).Text("Repayment schedule").FontSize(10).Bold();

                    if (schedule.Count == 0)
                    {
                        content.Item().PaddingTop(4)
                            .Text("Not available — loan amount, rate and tenure must all be set.")
                            .FontSize(8.5f).FontColor("#8a1f1f");
                    }
                    else
                    {
                        content.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(38);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                foreach (var title in new[] { "Month", "Opening", "EMI", "Principal", "Interest", "Closing" })
                                {
                                    header.Cell().Element(HeaderCell).Text(title).FontSize(8).Bold();
                                }
                            });

                            foreach (var row in schedule)
                            {
                                table.Cell().Element(BodyCell).Text(row.Month.ToString(CultureInfo.InvariantCulture)).FontSize(8);
                                table.Cell().Element(BodyCell).AlignRight().Text(Money(row.OpeningBalance)).FontSize(8);
                                table.Cell().Element(BodyCell).AlignRight().Text(Money(row.Emi)).FontSize(8);
                                table.Cell().Element(BodyCell).AlignRight().Text(Money(row.Principal)).FontSize(8);
                                table.Cell().Element(BodyCell).AlignRight().Text(Money(row.Interest)).FontSize(8);
                                table.Cell().Element(BodyCell).AlignRight().Text(Money(row.ClosingBalance)).FontSize(8);
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(footer =>
                {
                    footer.Span("Generated ").FontSize(7.5f).FontColor("#93999a");
                    footer.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
                        .FontSize(7.5f).FontColor("#93999a");
                    footer.Span("  ·  page ").FontSize(7.5f).FontColor("#93999a");
                    footer.CurrentPageNumber().FontSize(7.5f).FontColor("#93999a");
                    footer.Span(" / ").FontSize(7.5f).FontColor("#93999a");
                    footer.TotalPages().FontSize(7.5f).FontColor("#93999a");
                });
            });
        }).GeneratePdf();
    }

    private static void AddCostRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Element(BodyCell).Text(label);
        table.Cell().Element(BodyCell).AlignRight().Text(value).SemiBold();
    }

    private static void AddTile(TableDescriptor table, string label, string value)
    {
        table.Cell().Element(TileCell).Column(column =>
        {
            column.Item().Text(label).FontSize(7.5f).FontColor("#7d7d7d");
            column.Item().PaddingTop(2).Text(value).FontSize(11).Bold();
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background("#f2f2f2").BorderBottom(1).BorderColor("#d4d4d4").Padding(4);

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1).BorderColor("#eceef1").Padding(4);

    private static IContainer TileCell(IContainer container) =>
        container.PaddingRight(4).Border(1).BorderColor("#b6b6b6").Background("#efefef").Padding(6);

    private static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static string Money(decimal? amount) =>
        "₹" + Math.Round(amount ?? 0m, MidpointRounding.AwayFromZero).ToString("N0", IndianCulture);

    private static string Percent(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture) + "%";
}

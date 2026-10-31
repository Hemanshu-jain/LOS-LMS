using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LosLms.Services;

/// <summary>Everything the Disbursement Memo prints, gathered by the page so the document itself has no database access.</summary>
public sealed record DisbursementMemoData(
    string CompanyName,
    string ApplicationId,
    string? Borrower,
    decimal SanctionedAmount,
    decimal? Roi,
    int TenureMonths,
    IReadOnlyList<(string Head, decimal Amount, bool Waived)> Charges,
    decimal TradeAdvance,
    decimal NetDisbursement,
    string? DisbursementType,
    string? BeneficiaryName,
    string? BeneficiaryAccount,
    string? DisburseFromAccount,
    string? PaymentMode,
    DateOnly? ValueDate,
    string? Utr,
    string? Recommender,
    string? Approver,
    DateOnly? ApprovedOn,
    decimal Emi,
    DateOnly? FirstEmiDate,
    DateOnly? LastEmiDate,
    int Instalments,
    decimal TotalRepayable);

/// <summary>The one-page internal memo that authorises a disbursement: terms, deductions, who gets the money and from where, approvals, sign-off.</summary>
public static class DisbursementMemoPdf
{
    private static readonly CultureInfo Indian = CultureInfo.GetCultureInfo("en-IN");

    public static string FileNameFor(string applicationId) => $"Disbursement-memo-{applicationId}.pdf";

    public static byte[] Build(DisbursementMemoData d) =>
        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(t => t.FontSize(10.5f).FontColor("#1a1f29"));

            page.Header().Column(h =>
            {
                h.Item().Text("Disbursement Memo").FontSize(18).Bold();
                h.Item().Text(string.IsNullOrWhiteSpace(d.CompanyName) ? "" : d.CompanyName.Trim()).FontColor("#5a6270");
                h.Item().PaddingTop(6).LineHorizontal(1).LineColor("#1a1f29");
            });

            page.Content().PaddingVertical(12).Column(c =>
            {
                c.Spacing(10);

                Section(c, "Loan", t =>
                {
                    Row(t, "Application no.", d.ApplicationId);
                    Row(t, "Borrower", Show(d.Borrower));
                    Row(t, "Sanctioned amount", Money(d.SanctionedAmount));
                    Row(t, "Rate / tenure", $"{(d.Roi is { } r ? r.ToString("0.##", CultureInfo.InvariantCulture) + "% p.a." : "—")} · {(d.TenureMonths > 0 ? d.TenureMonths + " months" : "—")}");
                });

                Section(c, "Deductions and net disbursement", t =>
                {
                    Row(t, "Sanctioned amount", Money(d.SanctionedAmount));
                    foreach (var (head, amount, waived) in d.Charges)
                    {
                        Row(t, "Less: " + head + (waived ? " (waived)" : ""), waived ? "—" : "− " + Money(amount));
                    }

                    if (d.TradeAdvance > 0) { Row(t, "Less: trade advance already paid", "− " + Money(d.TradeAdvance)); }
                    Row(t, "Net disbursement", Money(d.NetDisbursement), bold: true);
                });

                Section(c, "Payment", t =>
                {
                    Row(t, "Disbursement type", Show(d.DisbursementType));
                    Row(t, "Beneficiary", Show(d.BeneficiaryName));
                    Row(t, "Beneficiary account", Show(d.BeneficiaryAccount));
                    Row(t, "Disburse from account", Show(d.DisburseFromAccount));
                    Row(t, "Payment mode", Show(d.PaymentMode));
                    Row(t, "Value date", d.ValueDate?.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) ?? "—");
                    Row(t, "UTR / cheque no.", Show(d.Utr));
                });

                Section(c, "Approvals", t =>
                {
                    Row(t, "Recommended by", Show(d.Recommender));
                    Row(t, "Approved by", Show(d.Approver) + (d.ApprovedOn is { } on ? " on " + on.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) : ""));
                });

                Section(c, "Repayment summary", t =>
                {
                    Row(t, "EMI", Money(d.Emi));
                    Row(t, "Instalments", d.Instalments > 0 ? d.Instalments.ToString(CultureInfo.InvariantCulture) : "—");
                    Row(t, "First / last EMI date", $"{Date(d.FirstEmiDate)} / {Date(d.LastEmiDate)}");
                    Row(t, "Total repayable", d.TotalRepayable > 0 ? Money(d.TotalRepayable) : "—");
                });

                c.Item().PaddingTop(28).Row(row =>
                {
                    foreach (var label in new[] { "Prepared by", "Checked by", "Authorised by" })
                    {
                        row.RelativeItem().PaddingRight(12).Column(col =>
                        {
                            col.Item().PaddingTop(26).LineHorizontal(1).LineColor("#1a1f29");
                            col.Item().Text(label).FontSize(9).FontColor("#5a6270");
                        });
                    }
                });
            });

            page.Footer().AlignCenter().Text($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor("#93999a");
        })).GeneratePdf();

    private static void Section(ColumnDescriptor c, string title, Action<TableDescriptor> rows)
    {
        c.Item().Column(s =>
        {
            s.Item().Text(title).SemiBold().FontSize(11.5f);
            s.Item().PaddingTop(3).Table(t =>
            {
                t.ColumnsDefinition(cols => { cols.RelativeColumn(2); cols.RelativeColumn(3); });
                rows(t);
            });
        });
    }

    private static void Row(TableDescriptor t, string label, string value, bool bold = false)
    {
        t.Cell().Element(Cell).Text(label).SemiBold();
        var v = t.Cell().Element(Cell).Text(value);
        if (bold) { v.Bold(); }
    }

    private static IContainer Cell(IContainer container) => container.BorderBottom(1).BorderColor("#eceef1").PaddingVertical(4);

    private static string Show(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s.Trim();

    private static string Date(DateOnly? d) => d?.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) ?? "—";

    private static string Money(decimal amount) => "₹" + Math.Round(amount, MidpointRounding.AwayFromZero).ToString("N0", Indian);
}

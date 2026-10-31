using System.Globalization;
using LosLms.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LosLms.Services;

/// <summary>Everything the agreement prints, gathered by the page so the document itself touches no database.</summary>
/// <param name="Charges">Approvals charges (processing fee and the rest); waived ones are shown as waived and not counted.</param>
/// <param name="Schedule">Repayment schedule with due dates (Schedule B).</param>
/// <param name="FirstEmi">Month-1 instalment (the first-EMI override if one was set).</param>
/// <param name="NetDisbursed">Sanctioned amount less charges and any advance already paid.</param>
/// <param name="BounceCharge">Charge for a dishonoured EMI from the company's Digio / collections settings; null when none is configured.</param>
/// <param name="LenderSignature">The lender's signature image; the agreement is not produced without it.</param>
public sealed record AgreementData(
    Company Lender,
    Application Application,
    ApprovalDecision Decision,
    IReadOnlyList<Party> Parties,
    SecurityDetail? Security,
    IReadOnlyList<Charge> Charges,
    IReadOnlyList<AmortizationRow> Schedule,
    decimal FirstEmi,
    decimal NetDisbursed,
    decimal TradeAdvance,
    DateOnly? ValueDate,
    DateOnly? FirstEmiDate,
    int? EmiDay,
    decimal? BounceCharge,
    decimal BounceGstPct,
    DateOnly ExecutionDate,
    byte[]? LenderSignature);

/// <summary>Fixed geometry of the signature page, shared by the layout and by <see cref="LoanAgreementSignatureBoxes"/> (PDF points, origin bottom-left).</summary>
public static class AgreementLayout
{
    public const float PageHeight = 841.89f;
    public const float Margin = 36f;
    public const float HeaderHeight = 80f;
    public const float FooterHeight = 22f;

    /// <summary>The signature page is always page 5: page 1 parties, pages 2-4 clauses, then this, then the schedules.</summary>
    public const int SignaturePage = 5;

    private const float ContentTop = Margin + HeaderHeight;
    public const float TitleHeight = 28f;
    public const float WitnessHeight = 44f;
    public const float LenderBlockHeight = 150f;
    public const float SlotHeight = 112f;
    public const float LabelHeight = 20f;
    public const float BoxHeight = 64f;
    public const float BoxWidth = 230f;

    public static readonly string[] SlotOrder = { "Applicant", "CoApplicant", "Guarantor" };

    /// <summary>Digio sign box for a party type (1-based page, origin bottom-left); null for an unknown type.</summary>
    public static SignBox? BoxFor(string partyType)
    {
        var slot = Array.IndexOf(SlotOrder, partyType);
        if (slot < 0) { return null; }

        var topFromPageTop = ContentTop + TitleHeight + WitnessHeight + LenderBlockHeight + slot * SlotHeight + LabelHeight;
        var ury = PageHeight - topFromPageTop;
        return new SignBox(SignaturePage, Margin, Math.Round(ury - BoxHeight, 1), Margin + BoxWidth, Math.Round(ury, 1));
    }
}

/// <summary>Tells the e-Sign service where each borrower signs on the generated agreement.</summary>
public sealed class LoanAgreementSignatureBoxes : IAgreementSignatureBoxes
{
    public IReadOnlyList<SignBox>? BoxesFor(string partyType) => AgreementLayout.BoxFor(partyType) is { } box ? new[] { box } : null;
}

/// <summary>
/// The loan agreement: letterhead on every page, parties, numbered clauses, a signature page, the RBI Key Fact Statement (Schedule A) and
/// the repayment schedule (Schedule B). QuestPDF, same pattern as <see cref="CamPdf"/>.
/// </summary>
/// <remarks>
/// Pages 1-5 are fixed (each one is "show entire"; a page whose text no longer fits fails loudly) so that the signature page is always page
/// <see cref="AgreementLayout.SignaturePage"/> and the Digio sign coordinates stay valid. The schedules come after it and may run to any length.
/// The clause wording lives in <see cref="AgreementClauses"/>, a default template for the company's counsel to review.
/// </remarks>
public static class LoanAgreementPdf
{
    private static readonly CultureInfo Indian = CultureInfo.GetCultureInfo("en-IN");
    private const string AsPerSanction = "As per sanction letter";
    private const string Ink = "#1a1f29";
    private const string Muted = "#5b6472";

    public static string FileNameFor(Application application, string? applicantName = null) =>
        DocumentNames.For(application.Id, applicantName ?? application.CustomerName, application.CreatedAt, DocumentNames.LoanAgreement);

    public static byte[] Build(AgreementData d) => Compose(d).GeneratePdf();

    /// <summary>The document itself (tests render its pages as images).</summary>
    internal static Document Compose(AgreementData d)
    {
        var parties = Ordered(d.Parties);
        var clauses = AgreementClauses.For(new ClauseContext(
            LenderName(d.Lender), d.Lender.RegisteredCity, SecurityParagraph(d.Security), GrievanceLine(d.Lender)));
        var kfs = LoanKfs.Compute(d.Schedule, d.Decision.SanctionedAmount ?? 0m, d.NetDisbursed,
            d.Charges.Where(c => !c.Waived).Sum(c => c.Amount + c.Gst), d.ValueDate);

        return Document.Create(document =>
        {
            // Page 1 — header block and parties
            document.Page(page => Frame(page, d, p1 => PartiesPage(p1, d, parties), showEntire: true));

            // Pages 2-4 — clauses
            for (var i = 0; i < AgreementClauses.PageStarts.Length; i++)
            {
                var from = AgreementClauses.PageStarts[i];
                var to = i + 1 < AgreementClauses.PageStarts.Length ? AgreementClauses.PageStarts[i + 1] - 1 : int.MaxValue;
                var group = clauses.Where(c => c.Number >= from && c.Number <= to).ToList();
                document.Page(page => Frame(page, d, c => ClausesPage(c, group), showEntire: true));
            }

            // Page 5 — signatures (fixed geometry, see AgreementLayout)
            document.Page(page => Frame(page, d, c => SignaturePage(c, d, parties), showEntire: true));

            // Schedule A — Key Fact Statement, Schedule B — repayment schedule (any length)
            document.Page(page => Frame(page, d, c => KeyFactStatement(c, d, kfs), showEntire: false));
            document.Page(page => Frame(page, d, c => RepaymentSchedule(c, d), showEntire: false));
        });
    }

    // ---------------------------------------------------------------- frame: letterhead + footer on every page

    private static void Frame(PageDescriptor page, AgreementData d, Action<ColumnDescriptor> body, bool showEntire)
    {
        page.Size(PageSizes.A4);
        page.Margin(AgreementLayout.Margin);
        page.DefaultTextStyle(t => t.FontSize(9.5f).FontColor(Ink));

        page.Header().Height(AgreementLayout.HeaderHeight).Column(h =>
        {
            h.Item().Text(LenderName(d.Lender)).FontSize(14).Bold();
            if (!string.IsNullOrWhiteSpace(d.Lender.Address)) { h.Item().Text(d.Lender.Address.Trim()).FontSize(8).FontColor(Muted).ClampLines(2); }
            var ids = Join(" · ", Labelled("CIN", d.Lender.Cin), Labelled("RBI NBFC Reg. No.", d.Lender.RbiNbfcRegNo), Labelled("GSTIN", d.Lender.Gstin));
            if (ids.Length > 0) { h.Item().Text(ids).FontSize(8).FontColor(Muted); }
            var contact = Join(" · ", Labelled("Tel", d.Lender.ContactPhone), Blank(d.Lender.ContactEmail), Blank(d.Lender.Website));
            if (contact.Length > 0) { h.Item().Text(contact).FontSize(8).FontColor(Muted); }
            h.Item().PaddingTop(3).LineHorizontal(1).LineColor(Ink);
        });

        var content = page.Content();
        if (showEntire)
        {
            content.ShowEntire().Column(body);
        }
        else
        {
            content.Column(body);
        }

        page.Footer().Height(AgreementLayout.FooterHeight).AlignBottom().Text(t =>
        {
            t.DefaultTextStyle(s => s.FontSize(8).FontColor("#93999a"));
            t.Span($"Application {d.Application.Id} · Agreement no. {d.Application.Id} · page ");
            t.CurrentPageNumber();
            t.Span(" of ");
            t.TotalPages();
        });
    }

    // ---------------------------------------------------------------- page 1

    private static void PartiesPage(ColumnDescriptor c, AgreementData d, IReadOnlyList<Party> parties)
    {
        c.Spacing(8);
        c.Item().PaddingTop(8).AlignCenter().Text("LOAN AGREEMENT").FontSize(16).Bold();

        c.Item().Table(t =>
        {
            t.ColumnsDefinition(cols => { cols.RelativeColumn(1); cols.RelativeColumn(2); });
            Row(t, "Agreement no.", d.Application.Id);
            Row(t, "Place of execution", Show(d.Lender.RegisteredCity));
            Row(t, "Date of execution", d.ExecutionDate.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture));
            Row(t, "Loan product", Show(d.Application.LoanProduct ?? d.Application.Scheme));
        });

        c.Item().Text(t =>
        {
            t.Span("This Loan Agreement (the \"Agreement\") is made at ");
            t.Span(Show(d.Lender.RegisteredCity)).SemiBold();
            t.Span(" on ");
            t.Span(d.ExecutionDate.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture)).SemiBold();
            t.Span(" between:");
        });

        c.Item().Text(t =>
        {
            t.Span("(1) ").SemiBold();
            t.Span(LenderName(d.Lender)).SemiBold();
            t.Span(" (\"the Lender\"), a company having its registered office at ");
            t.Span(Show(d.Lender.Address));
            var reg = Join(", ", Labelled("CIN", d.Lender.Cin), Labelled("registered with the Reserve Bank of India as a non-banking financial company under No.", d.Lender.RbiNbfcRegNo));
            if (reg.Length > 0) { t.Span(" (" + reg + ")"); }
            t.Span(", acting through its authorised signatory; and");
        });

        var n = 2;
        foreach (var p in parties)
        {
            var number = n++;
            c.Item().Border(0.5f).BorderColor("#c9ccd2").Padding(6).Column(b =>
            {
                b.Spacing(2);
                b.Item().Text(t =>
                {
                    t.Span($"({number}) {Show(p.FullName)}").SemiBold();
                    t.Span($"  —  {RoleLabel(p.PartyType)} ({PartyDefinition(p.PartyType)})").FontColor(Muted);
                });
                b.Item().Text($"Father / spouse: {Show(p.FatherSpouseName)}   ·   Date of birth: {DobAge(p.DateOfBirth, d.ExecutionDate)}");
                b.Item().Text($"Address: {Address(p)}");
                b.Item().Text($"PAN: {MaskPan(p.Pan)}   ·   Aadhaar: {MaskAadhaar(p.Aadhaar)}   ·   Mobile: {Show(p.Mobile)}   ·   Email: {Show(p.Email)}");
            });
        }

        c.Item().PaddingTop(4).Text(
            "The Borrower has asked the Lender for the loan described in the Key Fact Statement (Schedule A) and the Lender has agreed, on the terms and conditions below. "
            + "Where there is more than one Borrower, each is liable jointly and severally. A Guarantor guarantees the Borrower's obligations as clause 8 provides.");
    }

    // ---------------------------------------------------------------- pages 2-4

    private static void ClausesPage(ColumnDescriptor c, IReadOnlyList<Clause> clauses)
    {
        c.Spacing(6);
        foreach (var clause in clauses)
        {
            c.Item().PaddingTop(4).Text($"{clause.Number}.  {clause.Heading}").Bold().FontSize(10);
            foreach (var para in clause.Paragraphs)
            {
                c.Item().PaddingLeft(14).Text(para).FontSize(9).LineHeight(1.25f).Justify();
            }
        }
    }

    // ---------------------------------------------------------------- page 5

    private static void SignaturePage(ColumnDescriptor c, AgreementData d, IReadOnlyList<Party> parties)
    {
        c.Item().Height(AgreementLayout.TitleHeight).Text("SIGNATURE PAGE").FontSize(13).Bold();
        c.Item().Height(AgreementLayout.WitnessHeight).Text(
            "IN WITNESS WHEREOF the parties have executed this Agreement on the date written at the top of the first page. "
            + $"Each Borrower and Guarantor signs electronically using Aadhaar e-Sign, as clause {AgreementClauses.ElectronicExecutionClause} provides.").FontSize(9);

        c.Item().Height(AgreementLayout.LenderBlockHeight).Column(l =>
        {
            l.Item().Text($"For {LenderName(d.Lender)}").SemiBold();
            l.Item().Height(60).AlignLeft().PaddingTop(4).Element(e =>
            {
                if (d.LenderSignature is { Length: > 0 }) { e.Image(d.LenderSignature).FitArea(); }
            });
            l.Item().LineHorizontal(0.7f).LineColor(Ink);
            l.Item().Text($"Authorised signatory: {Show(d.Lender.AuthorisedSignatoryName)}").FontSize(9);
            l.Item().Text($"Designation: {Show(d.Lender.AuthorisedSignatoryDesignation)}").FontSize(9).FontColor(Muted);
            l.Item().Text($"Date: {d.ExecutionDate:dd MMMM yyyy}   ·   Place: {Show(d.Lender.RegisteredCity)}").FontSize(9).FontColor(Muted);
        });

        foreach (var slot in AgreementLayout.SlotOrder)
        {
            var party = parties.FirstOrDefault(p => p.PartyType == slot);
            c.Item().Height(AgreementLayout.SlotHeight).Column(s =>
            {
                if (party is null) { return; }
                s.Item().Height(AgreementLayout.LabelHeight).Text($"{RoleLabel(slot)}: {Show(party.FullName)}").SemiBold();
                s.Item().Height(AgreementLayout.BoxHeight).Width(AgreementLayout.BoxWidth).Border(0.7f).BorderColor("#9aa0aa")
                    .AlignMiddle().AlignCenter().Text("Signed electronically via Aadhaar e-Sign").FontSize(8).FontColor("#9aa0aa");
                s.Item().PaddingTop(3).Text($"Name: {Show(party.FullName)}   ·   Date: ________________").FontSize(8.5f).FontColor(Muted);
            });
        }
    }

    // ---------------------------------------------------------------- Schedule A — Key Fact Statement

    private static void KeyFactStatement(ColumnDescriptor c, AgreementData d, LoanKfs kfs)
    {
        c.Spacing(6);
        c.Item().PaddingTop(6).Text("SCHEDULE A — KEY FACT STATEMENT").FontSize(13).Bold();
        c.Item().Text($"Loan agreement no. {d.Application.Id}  ·  Borrower: {Show(d.Application.CustomerName)}  ·  Lender: {LenderName(d.Lender)}").FontSize(8.5f).FontColor(Muted);

        var live = d.Charges.Where(x => !x.Waived).ToList();
        var processing = live.Where(x => x.Head.Contains("processing", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Amount + x.Gst);
        var firstEmiDate = d.FirstEmiDate ?? d.Schedule.FirstOrDefault()?.DueDate;
        var emiDay = d.EmiDay ?? firstEmiDate?.Day;

        c.Item().Table(t =>
        {
            t.ColumnsDefinition(cols => { cols.ConstantColumn(28); cols.RelativeColumn(5); cols.RelativeColumn(5); });
            var i = 1;
            void K(string label, string value)
            {
                t.Cell().Element(Cell).Text((i++).ToString(CultureInfo.InvariantCulture));
                t.Cell().Element(Cell).Text(label).SemiBold();
                t.Cell().Element(Cell).Text(value);
            }

            K("Sanctioned loan amount", Money(kfs.Sanctioned));
            K("Net amount disbursed to the Borrower", Money(kfs.NetDisbursed));
            K("Loan term", d.Decision.SanctionedTenure is { } tenure and > 0 ? $"{tenure} months" : AsPerSanction);
            K("Type of interest rate", "Fixed");
            K("Rate of interest", d.Decision.SanctionedRoi is { } roi ? $"{roi.ToString("0.##", CultureInfo.InvariantCulture)}% per annum" : AsPerSanction);
            K("Equated monthly instalment (EMI)", d.Schedule.Count > 0 ? Money(d.FirstEmi) : AsPerSanction);
            K("Number of instalments", d.Schedule.Count > 0 ? d.Schedule.Count.ToString(CultureInfo.InvariantCulture) : AsPerSanction);
            K("First EMI date", firstEmiDate?.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture) ?? AsPerSanction);
            K("EMI due day of the month", emiDay is { } day ? $"{day}" : AsPerSanction);
            K("Total interest payable over the term", d.Schedule.Count > 0 ? Money(kfs.TotalInterest) : AsPerSanction);
            K("Total amount repayable (principal + interest)", d.Schedule.Count > 0 ? Money(kfs.TotalRepayable) : AsPerSanction);
            K("Processing fee (incl. GST)", processing > 0 ? Money(processing) : AsPerSanction);
            K("Total fees and charges deducted at disbursement (incl. GST)", Money(kfs.TotalFees));
            if (d.TradeAdvance > 0) { K("Advance already paid to the dealer / seller", Money(d.TradeAdvance)); }
            K("Annual percentage rate (APR), including fees", kfs.AprPct is { } apr ? $"{apr.ToString("0.00", CultureInfo.InvariantCulture)}% per annum" : AsPerSanction);
            K("Penal charges on default", AsPerSanction);
            K("Charge for a dishonoured EMI", d.BounceCharge is { } b && b > 0
                ? Money(b + Math.Round(b * d.BounceGstPct / 100m, 2)) + " per instance (incl. GST)" : AsPerSanction);
            K("Prepayment / foreclosure charges", AsPerSanction);
            K("Cooling-off / look-up period", AsPerSanction);
            K("Grievance redressal officer", GrievanceLine(d.Lender));
            K("Recovery agents", $"Recovery is carried out in line with the RBI Fair Practices Code (clause {AgreementClauses.RecoveryClause}); agents follow the Lender's code of conduct.");
        });

        if (d.Charges.Count > 0)
        {
            c.Item().PaddingTop(4).Text("Fees and charges (from the sanction)").SemiBold();
            c.Item().Table(t =>
            {
                t.ColumnsDefinition(cols => { cols.RelativeColumn(4); cols.RelativeColumn(2); cols.RelativeColumn(2); cols.RelativeColumn(2); });
                foreach (var h in new[] { "Charge", "Amount", "GST", "Total" }) { t.Cell().Element(HeadCell).Text(h).Bold(); }
                foreach (var ch in d.Charges)
                {
                    t.Cell().Element(Cell).Text(ch.Head + (ch.Waived ? " (waived)" : ""));
                    t.Cell().Element(Cell).Text(ch.Waived ? "—" : Money(ch.Amount));
                    t.Cell().Element(Cell).Text(ch.Waived ? "—" : Money(ch.Gst));
                    t.Cell().Element(Cell).Text(ch.Waived ? "—" : Money(ch.Amount + ch.Gst));
                }
            });
        }

        c.Item().PaddingTop(4).Text(
            "The APR is the effective annual rate at which the instalments repay the amount the Borrower actually receives, counted from the value date of disbursement; "
            + "it therefore includes the processing fee and other charges deducted up front. A figure shown as \"As per sanction letter\" is not held in the Lender's records for this loan.")
            .FontSize(8).FontColor(Muted);
    }

    // ---------------------------------------------------------------- Schedule B — repayment schedule

    private static void RepaymentSchedule(ColumnDescriptor c, AgreementData d)
    {
        c.Spacing(6);
        c.Item().PaddingTop(6).Text("SCHEDULE B — REPAYMENT SCHEDULE").FontSize(13).Bold();
        if (d.Schedule.Count == 0)
        {
            c.Item().Text("The repayment schedule is as per the sanction letter.");
            return;
        }

        c.Item().Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(34); cols.RelativeColumn(2); cols.RelativeColumn(2); cols.RelativeColumn(2); cols.RelativeColumn(2); cols.RelativeColumn(2);
            });
            t.Header(h =>
            {
                foreach (var x in new[] { "No.", "Due date", "EMI", "Principal", "Interest", "Balance" }) { h.Cell().Element(HeadCell).Text(x).Bold(); }
            });
            foreach (var r in d.Schedule)
            {
                t.Cell().Element(Cell).Text(r.Month.ToString(CultureInfo.InvariantCulture));
                t.Cell().Element(Cell).Text(r.DueDate?.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) ?? "—");
                t.Cell().Element(Cell).AlignRight().Text(Money2(r.Emi));
                t.Cell().Element(Cell).AlignRight().Text(Money2(r.Principal));
                t.Cell().Element(Cell).AlignRight().Text(Money2(r.Interest));
                t.Cell().Element(Cell).AlignRight().Text(Money2(r.ClosingBalance));
            }
        });
    }

    // ---------------------------------------------------------------- helpers

    private static IReadOnlyList<Party> Ordered(IReadOnlyList<Party> parties) =>
        parties.Where(p => AgreementLayout.SlotOrder.Contains(p.PartyType) && !string.IsNullOrWhiteSpace(p.FullName))
            .OrderBy(p => Array.IndexOf(AgreementLayout.SlotOrder, p.PartyType)).ThenBy(p => p.Id).ToList();

    private static string SecurityParagraph(SecurityDetail? s)
    {
        if (s is null)
        {
            return "The Loan is secured as stated in the sanction letter.";
        }

        if (string.Equals(s.AssetType, "Property", StringComparison.OrdinalIgnoreCase))
        {
            return "As security for the Loan the Borrower creates a mortgage in favour of the Lender over the immovable property described below (\"the Security\"). "
                + $"Property type: {Show(s.PropertyType)}. Address: {Show(s.PropertyAddress)}.";
        }

        return "As security for the Loan the Borrower hypothecates to the Lender the vehicle described below (\"the Security\"), together with its accessories, and shall have the Lender's hypothecation endorsed on the registration certificate. "
            + $"Make / model: {Show(s.MakeModel)}; year of manufacture: {Show(s.MfgYear)}; registration no.: {Show(s.RegNo)}; chassis no.: {Show(s.ChassisNo)}; engine no.: {Show(s.EngineNo)}.";
    }

    private static string GrievanceLine(Company c)
    {
        var line = Join(", ", Blank(c.GrievanceOfficerName), Labelled("phone", c.GrievanceOfficerPhone), Labelled("email", c.GrievanceOfficerEmail));
        return line.Length > 0 ? line + "." : AsPerSanction;
    }

    private static string LenderName(Company c) => string.IsNullOrWhiteSpace(c.Name) ? "the Lender" : c.Name.Trim();

    /// <summary>How the party is defined on page 1: a guarantor is "the Guarantor", never a borrower.</summary>
    private static string PartyDefinition(string partyType) => partyType switch
    {
        "Guarantor" => "\"the Guarantor\"",
        "CoApplicant" => "\"the Borrower\", jointly and severally",
        _ => "\"the Borrower\"",
    };

    private static string RoleLabel(string partyType) => partyType switch
    {
        "CoApplicant" => "Co-Applicant",
        "Guarantor" => "Guarantor",
        _ => "Applicant",
    };

    private static string Address(Party p) =>
        Join(", ", Blank(p.Address1), Blank(p.Address2), Blank(p.City), Join(" ", Blank(p.State), Blank(p.PinCode))) is { Length: > 0 } a ? a : "—";

    private static string DobAge(DateOnly? dob, DateOnly on)
    {
        if (dob is not { } d) { return "—"; }
        var age = on.Year - d.Year - (on < d.AddYears(on.Year - d.Year) ? 1 : 0);
        return $"{d:dd MMM yyyy} (age {age})";
    }

    /// <summary>PAN with the first five and last character hidden: XXXXX1234X.</summary>
    public static string MaskPan(string? pan) =>
        pan is { Length: 10 } ? "XXXXX" + pan.Substring(5, 4).ToUpperInvariant() + "X" : "—";

    /// <summary>Aadhaar last four digits only.</summary>
    public static string MaskAadhaar(string? aadhaar)
    {
        var digits = new string((aadhaar ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length >= 4 ? "XXXX XXXX " + digits[^4..] : "—";
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string? Labelled(string label, string? value) => Blank(value) is { } v ? $"{label} {v}" : null;

    private static string Join(string sep, params string?[] parts) => string.Join(sep, parts.Where(p => !string.IsNullOrWhiteSpace(p)));

    private static string Show(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    private static void Row(TableDescriptor t, string label, string value)
    {
        t.Cell().Element(Cell).Text(label).SemiBold();
        t.Cell().Element(Cell).Text(value);
    }

    private static IContainer Cell(IContainer c) => c.BorderBottom(0.5f).BorderColor("#e1e4e9").PaddingVertical(3).PaddingHorizontal(3);

    private static IContainer HeadCell(IContainer c) => c.Background("#f2f2f2").BorderBottom(0.7f).BorderColor("#c9ccd2").Padding(3);

    private static string Money(decimal amount) => "₹" + Math.Round(amount, MidpointRounding.AwayFromZero).ToString("N0", Indian);

    private static string Money2(decimal amount) => amount.ToString("N2", Indian);
}

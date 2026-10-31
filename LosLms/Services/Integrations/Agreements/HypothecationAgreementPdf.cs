using System.Globalization;
using System.Text.RegularExpressions;
using LosLms.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LosLms.Services.Agreements;

/// <summary>
/// The client's printed "Agreement of Hypothecation" form, rebuilt page for page: cover, the 27-article Hypothecation cum Loan Agreement, the
/// two schedules, the proposal form, the Irrevocable Power of Attorney, the Guarantee deed and the Agreement of Arbitration. Every blank of
/// the form is filled from the application; a blank the app has no value for stays a printed blank line.
/// </summary>
/// <remarks>
/// Each printed page is exactly one PDF page, so the page numbers (and the e-Sign coordinates in <see cref="HypothecationLayout"/>) match the
/// form: a page whose filled-in text is too long for it is set in a slightly smaller type until it fits. Our Key Fact Statement and
/// repayment schedule (RBI requirements the printed form has no equivalent of) follow the form as Schedules A and B. The wording itself is in
/// <see cref="HypothecationText"/>.
/// </remarks>
public static class HypothecationAgreementPdf
{
    private const string Cream = "#fbfbea";
    private const string Ink = "#141414";
    private const float BaseFont = 11.0f;
    private static readonly float[] FitScales = { 1.0f, 0.97f, 0.94f, 0.91f, 0.88f, 0.85f, 0.82f, 0.79f, 0.76f };
    private static readonly Regex Token = new(@"\*\*(.+?)\*\*|\{(\w+)(?::(\d+)(\.)?)?\}", RegexOptions.Compiled);

    private sealed record Ctx(Dictionary<string, string> Values, AgreementData Data, float Scale)
    {
        public float Fs(float size = BaseFont) => size * Scale;
        public float Sp(float points) => points * Scale;
    }

    public static string FileNameFor(Application application, string? applicantName = null) =>
        DocumentNames.For(application.Id, applicantName ?? application.CustomerName, application.CreatedAt, DocumentNames.LoanAgreement);

    public static byte[] Build(AgreementData d) => Compose(d).GeneratePdf();

    internal static Document Compose(AgreementData d)
    {
        var values = HypothecationValues.Build(d);
        var scales = HypothecationForm.Pages.ToDictionary(p => p.Number, p => FitScale(p, d, values));

        return Document.Create(document =>
        {
            foreach (var form in HypothecationForm.Pages)
            {
                var cx = new Ctx(values, d, scales[form.Number]);
                document.Page(page => BuildPage(page, form, cx));
            }

            LoanAgreementPdf.AddScheduleAB(document, d);
        });
    }

    /// <summary>The largest type scale at which the page still fits on one sheet (pages with fixed art are never rescaled).</summary>
    private static float FitScale(FormPage form, AgreementData d, Dictionary<string, string> values)
    {
        if (form.Flag is "cover" or "blank" or "back") { return 1f; }

        foreach (var scale in FitScales)
        {
            try
            {
                var cx = new Ctx(values, d, scale);
                Document.Create(doc => doc.Page(page => BuildPage(page, form, cx))).GeneratePdf();
                return scale;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // too big at this scale: try a smaller one
            }
        }

        throw new InvalidOperationException($"Page {form.Number} of the hypothecation form does not fit even at the smallest type; shorten the filled-in text.");
    }

    // ---------------------------------------------------------------- page frame

    private static void BuildPage(PageDescriptor page, FormPage form, Ctx cx)
    {
        page.Size(new PageSize(HypothecationLayout.PageW, HypothecationLayout.PageH));
        page.MarginHorizontal(HypothecationLayout.MarginX);
        page.MarginTop(HypothecationLayout.MarginTop);
        page.MarginBottom(HypothecationLayout.MarginBottom);
        page.PageColor(Cream);
        page.DefaultTextStyle(t => t.FontSize(cx.Fs()).LineHeight(1.17f).FontColor(Ink));

        switch (form.Flag)
        {
            case "cover":
                Cover(page, cx);
                return;
            case "blank":
            case "back":
                page.Content().Element(e => e.Height(10));
                return;
        }

        if (form.Printed is { } printed)
        {
            page.Header().PaddingBottom(14).AlignCenter().Text(printed.ToString(CultureInfo.InvariantCulture)).Bold().FontSize(10);
        }

        page.Content().ShowEntire().Column(col =>
        {
            col.Spacing(0);
            if (form.Flag == "schedule2") { Schedule2(col, form, cx); }
            else { Lines(col, form.Body, cx, form.Flag); }
        });

        Footer(page, form, cx);
        Stamps(page, form.Number, cx);
    }

    private static void Footer(PageDescriptor page, FormPage form, Ctx cx)
    {
        var fixedCx = cx with { Scale = 1f };
        switch (form.Flag)
        {
            case "arbitration":
                page.Footer().Height(HypothecationLayout.ArbFooterH).Column(f =>
                {
                    f.Item().Height(34);
                    f.Item().Row(r =>
                    {
                        var parts = form.Foot.FirstOrDefault(l => l.Kind == "ARBFOOT")?.Parts ?? new[] { "FINANCIER", "BORROWER", "GUARANTOR/S" };
                        r.RelativeItem().Text(parts[0]).FontSize(9.5f);
                        r.RelativeItem().AlignCenter().Text(parts[1]).FontSize(9.5f);
                        r.RelativeItem().AlignRight().Text(parts[2]).FontSize(9.5f);
                    });
                });
                return;
            case "proposal":
                return;
            case "proposal2":
                page.Footer().Height(HypothecationLayout.P16FooterH).Column(f =>
                {
                    Lines(f, form.Foot, fixedCx, form.Flag, footer: true);
                    f.Item().Height(HypothecationLayout.P16Bottom);
                });
                return;
            case "poa2":
                page.Footer().Height(HypothecationLayout.P18FooterH).Column(f =>
                {
                    f.Item().Height(HypothecationLayout.P18BlockH).Column(b => Lines(b, form.Foot, fixedCx, form.Flag, footer: true));
                    StdFooterRow(f);
                });
                return;
            case "guarantee2":
                page.Footer().Height(HypothecationLayout.P20FooterH).Column(f =>
                {
                    f.Item().Height(HypothecationLayout.P20BlockH).Column(b => Lines(b, form.Foot, fixedCx, form.Flag, footer: true));
                    StdFooterRow(f);
                });
                return;
            default:
                if (form.Flag == "schedule2" || form.Number >= 3)
                {
                    page.Footer().Height(HypothecationLayout.StdFooterH).Column(StdFooterRow);
                }

                return;
        }
    }

    /// <summary>The "LENDER … BORROWER" label row at the foot of every page; the signature boxes sit just above it.</summary>
    private static void StdFooterRow(ColumnDescriptor f)
    {
        f.Item().Height(HypothecationLayout.StdFooterH - HypothecationLayout.LabelRowH);
        f.Item().Height(HypothecationLayout.LabelRowH).Row(r =>
        {
            r.RelativeItem().PaddingLeft(36).Text("LENDER").Bold().Italic().FontSize(13f);
            r.RelativeItem().AlignRight().PaddingRight(36).Text("BORROWER").Bold().Italic().FontSize(13f);
        });
    }

    /// <summary>Outlined sign-here boxes (exactly where <see cref="HypothecationLayout"/> tells the e-Sign service to sign) and the lender's stamped signature.</summary>
    private static void Stamps(PageDescriptor page, int pageNumber, Ctx cx)
    {
        var hasCo = cx.Data.Parties.Any(p => p.PartyType == "CoApplicant" && !string.IsNullOrWhiteSpace(p.FullName));
        var hasGuarantor = cx.Data.Parties.Any(p => p.PartyType == "Guarantor" && !string.IsNullOrWhiteSpace(p.FullName));
        var boxes = new List<(SignBox Box, string Caption)>();
        foreach (var (type, caption, present) in new[] { ("Applicant", "Borrower signs here", true), ("CoApplicant", "Co-borrower signs here", hasCo), ("Guarantor", "Guarantor signs here", hasGuarantor) })
        {
            if (!present) { continue; }
            foreach (var b in HypothecationLayout.BoxesFor(type)!.Where(b => b.Page == pageNumber)) { boxes.Add((b, caption)); }
        }

        var stamp = HypothecationLayout.LenderStamp(pageNumber);
        var signature = cx.Data.LenderSignature is { Length: > 0 } bytes ? bytes : null;
        if (boxes.Count == 0 && (stamp is null || signature is null)) { return; }

        page.Foreground().Layers(layers =>
        {
            layers.PrimaryLayer().Element(e => e.Height(1));
            foreach (var (box, caption) in boxes)
            {
                layers.Layer().TranslateX((float)box.Llx).TranslateY(HypothecationLayout.PageH - (float)box.Ury)
                    .Width((float)(box.Urx - box.Llx)).Height((float)(box.Ury - box.Lly))
                    .Border(0.4f).BorderColor("#8a8a7a").AlignBottom().AlignRight().Padding(1.5f)
                    .Text(caption).FontSize(5.5f).FontColor("#8a8a7a");
            }

            if (stamp is { } s && signature is not null)
            {
                layers.Layer().TranslateX((float)s.Llx).TranslateY(HypothecationLayout.PageH - (float)s.Ury)
                    .Width((float)(s.Urx - s.Llx)).Height((float)(s.Ury - s.Lly)).Image(signature).FitArea();
            }
        });
    }

    // ---------------------------------------------------------------- cover

    private static void Cover(PageDescriptor page, Ctx cx)
    {
        string V(string k) => cx.Values.GetValueOrDefault(k) ?? string.Empty;
        var borrowers = V("borrower_names_1") + (V("borrower_names_2").Length > 0 ? " " + V("borrower_names_2") : string.Empty);

        page.Content().Column(c =>
        {
            c.Item().Height(190);
            c.Item().BorderTop(0.6f).BorderBottom(0.6f).Height(15).Svg(StarBorderSvg());
            c.Item().Height(100);
            c.Item().AlignCenter().Text("AGREEMENT OF HYPOTHECATION").Bold().FontSize(23);
            c.Item().Height(36);
            c.Item().AlignCenter().Text("BETWEEN").Bold().FontSize(11);
            c.Item().Height(36);
            c.Item().Height(20).AlignBottom().Text(V("lender_line").Length > 0 ? V("lender_line") : V("lender_name")).Bold().FontSize(10.5f);
            c.Item().BorderTop(0.7f).AlignRight().Text("LENDER").Bold().FontSize(11);
            c.Item().Height(150);
            c.Item().AlignCenter().Text("AND").Bold().FontSize(11);
            c.Item().Height(18);
            c.Item().Height(20).AlignBottom().Text(borrowers).Bold().FontSize(10.5f);
            c.Item().BorderTop(0.7f).AlignRight().Text("BORROWER").Bold().FontSize(11);
            c.Item().Height(50);
            c.Item().Row(r =>
            {
                r.RelativeItem().Text(t => { t.Span("Vehicle No. ").Bold(); t.Span(V("reg_no").Length > 0 ? V("reg_no") : new string('_', 24)).Underline(); });
                r.RelativeItem().Text(t => { t.Span("Make ").Bold(); t.Span(V("make").Length > 0 ? V("make") : new string('_', 24)).Underline(); });
            });
            c.Item().Height(4);
            c.Item().BorderTop(0.6f).BorderBottom(0.6f).Height(15).Svg(StarBorderSvg());
        });
    }

    /// <summary>The cover's star border: a row of small six-armed stars, drawn rather than typed so it needs no special font.</summary>
    private static string StarBorderSvg()
    {
        const int count = 52;
        var width = HypothecationLayout.PageW - 2 * HypothecationLayout.MarginX;
        var step = width / count;
        var sb = new System.Text.StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width:0.##}\" height=\"15\" viewBox=\"0 0 {width:0.##} 15\">");
        for (var i = 0; i < count; i++)
        {
            var cx = step * (i + 0.5);
            foreach (var angle in new[] { 0.0, 60.0, 120.0 })
            {
                var rad = angle * Math.PI / 180;
                var dx = 4.2 * Math.Cos(rad);
                var dy = 4.2 * Math.Sin(rad);
                sb.Append(CultureInfo.InvariantCulture, $"<line x1=\"{cx - dx:0.##}\" y1=\"{7.5 - dy:0.##}\" x2=\"{cx + dx:0.##}\" y2=\"{7.5 + dy:0.##}\" stroke=\"#141414\" stroke-width=\"1.1\"/>");
            }

            sb.Append(CultureInfo.InvariantCulture, $"<circle cx=\"{cx:0.##}\" cy=\"7.5\" r=\"1.1\" fill=\"#141414\"/>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    // ---------------------------------------------------------------- generic lines

    private static void Lines(ColumnDescriptor col, IEnumerable<FormLine> lines, Ctx cx, string flag, bool footer = false)
    {
        var all = lines.ToList();
        var labelW = Math.Max(30f, all.Where(l => l.Kind is "H" or "HB").Select(l => l.Part(0).Length * 5.4f + 6f).DefaultIfEmpty(0).Max());
        foreach (var l in all)
        {
            if (footer) { FootLine(col, l, cx, labelW); }
            else { Line(col, l, cx, labelW); }
        }
    }

    /// <summary>
    /// Lines of a fixed footer block get fixed heights, so the signature boxes in <see cref="HypothecationLayout"/> always sit exactly where the
    /// printed lines leave room for them.
    /// </summary>
    private static void FootLine(ColumnDescriptor col, FormLine l, Ctx cx, float labelW)
    {
        switch (l.Kind)
        {
            case "S":
            case "SB":
                col.Item().Height(18).Text(t => { t.Justify(); Inline(t, l.Text, cx, bold: l.Kind == "SB", size: cx.Fs(10.6f)); });
                break;
            case "SBL":
                col.Item().Height(16).Text(t => Inline(t, l.Text, cx, bold: true, size: cx.Fs(10.8f)));
                break;
            case "LB":
                col.Item().Height(l.Text.Contains('{') ? 20 : 16).Text(t => Inline(t, l.Text, cx, bold: true, size: cx.Fs(10.8f)));
                break;
            case "SP":
                col.Item().Height(float.Parse(l.Text, CultureInfo.InvariantCulture));
                break;
            case "SIGBOX":
                col.Item().Height(44);
                break;
            case "NOTARY":
                col.Item().Height(24).AlignCenter().AlignBottom().Text(l.Text).Bold().FontSize(9.4f);
                break;
            default:
                Line(col, l, cx, labelW);
                break;
        }
    }

    private static void Line(ColumnDescriptor col, FormLine l, Ctx cx, float labelW)
    {
        var fs = cx.Fs();
        switch (l.Kind)
        {
            case "C":
                col.Item().AlignCenter().PaddingVertical(cx.Sp(3)).Text(l.Text).Bold().FontSize(cx.Fs(12f));
                break;
            case "CB":
                col.Item().PaddingTop(cx.Sp(5)).PaddingBottom(cx.Sp(2)).AlignCenter().Text(t => Inline(t, l.Text, cx, bold: true, size: cx.Fs(11.5f)));
                break;
            case "CT":
                col.Item().PaddingBottom(cx.Sp(3)).AlignCenter().Text(l.Text).Bold().FontSize(cx.Fs(12.5f));
                break;
            case "TITLE":
                col.Item().PaddingBottom(cx.Sp(10)).AlignCenter().Text(l.Text).Bold().Underline().FontSize(cx.Fs(14f));
                break;
            case "L":
                col.Item().PaddingBottom(cx.Sp(2.5f)).Text(t => { t.Justify(); Inline(t, l.Text, cx); });
                break;
            case "LB":
                col.Item().PaddingTop(cx.Sp(2)).PaddingBottom(cx.Sp(2)).Text(t => { t.Justify(); Inline(t, l.Text, cx, bold: true); });
                break;
            case "LH":
                col.Item().PaddingTop(cx.Sp(6)).PaddingBottom(cx.Sp(3)).Text(t => Inline(t, l.Text, cx, bold: true, size: cx.Fs(11.5f)));
                break;
            case "H":
            case "HB":
                col.Item().PaddingBottom(cx.Sp(2.5f)).Row(r =>
                {
                    r.ConstantItem(labelW).Text(t => Inline(t, l.Part(0), cx, bold: l.Kind == "HB"));
                    r.RelativeItem().Text(t => { t.Justify(); Inline(t, l.Part(1), cx); });
                });
                break;
            case "H2":
                col.Item().PaddingBottom(cx.Sp(2.5f)).Row(r =>
                {
                    r.ConstantItem(26).Text(l.Part(0));
                    r.ConstantItem(26).Text(l.Part(1));
                    r.RelativeItem().Text(t => { t.Justify(); Inline(t, l.Part(2), cx); });
                });
                break;
            case "A":
                col.Item().PaddingBottom(cx.Sp(2.5f)).Row(r =>
                {
                    r.ConstantItem(34);
                    r.ConstantItem(30).Text(l.Part(0));
                    r.RelativeItem().Text(t => { t.Justify(); Inline(t, l.Part(1), cx); });
                });
                break;
            case "N":
                col.Item().PaddingTop(cx.Sp(3)).Row(r =>
                {
                    r.ConstantItem(labelW).Text(l.Part(0));
                    r.RelativeItem().Text(l.Part(1)).Bold();
                });
                break;
            case "Q":
                col.Item().PaddingLeft(labelW).PaddingBottom(cx.Sp(2.5f)).Text(t => { t.Justify(); Inline(t, l.Text, cx); });
                break;
            case "Q2":
                col.Item().PaddingLeft(64).PaddingBottom(cx.Sp(2.5f)).Text(t => { t.Justify(); Inline(t, l.Text, cx); });
                break;
            case "S":
                col.Item().PaddingBottom(cx.Sp(5)).Text(t => { t.Justify(); Inline(t, l.Text, cx, size: cx.Fs(10.6f)); });
                break;
            case "SB":
                col.Item().PaddingBottom(cx.Sp(5)).Text(t => { t.Justify(); Inline(t, l.Text, cx, bold: true, size: cx.Fs(10.8f)); });
                break;
            case "SBL":
                col.Item().PaddingBottom(cx.Sp(2)).Text(t => Inline(t, l.Text, cx, bold: true, size: cx.Fs(10.8f)));
                break;
            case "SC":
                col.Item().PaddingBottom(cx.Sp(5)).AlignCenter().Text(t => Inline(t, l.Text, cx, size: cx.Fs(10.6f)));
                break;
            case "SP":
                col.Item().Height(cx.Sp(float.Parse(l.Text, CultureInfo.InvariantCulture)));
                break;
            case "ROW":
            case "ROWH":
                ScheduleRow(col, l, cx);
                break;
            case "PTITLE":
                col.Item().PaddingTop(cx.Sp(14)).PaddingBottom(cx.Sp(8)).Text(l.Text).FontSize(cx.Fs(10.2f));
                break;
            case "PL":
                col.Item().PaddingBottom(cx.Sp(8)).Text(t => Inline(t, l.Text, cx, size: cx.Fs(11.8f)));
                break;
            case "PIND":
                col.Item().PaddingLeft(14).PaddingBottom(cx.Sp(8)).Text(t => { t.Justify(); Inline(t, l.Text, cx, size: cx.Fs(11.4f)); });
                break;
            case "PIND2":
                col.Item().Height(56).PaddingTop(2).Text(t => { t.Justify(); Inline(t, l.Text, cx, size: 8.8f); });
                break;
            case "PF":
                ProposalRow(col, l, cx);
                break;
            case "PCHOICE":
                ProposalChoice(col, l, cx);
                break;
            case "SIGLINE":
                col.Item().Height(44);
                col.Item().Height(16).Row(r =>
                {
                    r.RelativeItem().Text(l.Part(0)).FontSize(11.8f);
                    r.RelativeItem().AlignRight().Text(l.Part(1)).FontSize(11.8f);
                });
                break;
            case "PHOTOS":
                col.Item().Height(84).PaddingTop(6).Row(r =>
                {
                    r.ConstantItem(70);
                    r.ConstantItem(86).Border(0.6f).AlignCenter().AlignMiddle().Text(l.Part(0)).FontSize(8.6f).AlignCenter();
                    r.RelativeItem();
                    r.ConstantItem(86).Border(0.6f).AlignCenter().AlignMiddle().Text(l.Part(1)).FontSize(8.6f).AlignCenter();
                    r.ConstantItem(70);
                });
                break;
            case "GSIGN":
                col.Item().Height(36).Row(r =>
                {
                    r.ConstantItem(34).AlignBottom().Text(l.Part(0)).Bold();
                    r.RelativeItem().AlignBottom().Text(t => Inline(t, l.Part(1), cx, bold: true));
                });
                break;
            case "SIGBOX":
                break;
            case "NOTARY":
                col.Item().PaddingTop(4).AlignCenter().Text(l.Text).Bold().FontSize(8.6f);
                break;
            case "STAMPBOX":
                col.Item().Row(r =>
                {
                    r.RelativeItem().AlignBottom().PaddingBottom(4);
                    r.ConstantItem(98).Height(66).Border(0.7f).AlignCenter().AlignMiddle().Text(l.Text.Replace(" ", "\n")).FontSize(9f).AlignCenter();
                });
                break;
            case "ARBSCHEDULE":
                ArbitrationSchedule(col, cx);
                break;
            default:
                break;
        }
    }

    // ---------------------------------------------------------------- inline text with blanks

    private static void Inline(TextDescriptor t, string s, Ctx cx, bool bold = false, float? size = null)
    {
        var pos = 0;
        foreach (Match m in Token.Matches(s))
        {
            if (m.Index > pos) { Add(t, s[pos..m.Index], bold, size); }
            if (m.Groups[1].Success)
            {
                Add(t, m.Groups[1].Value, true, size);
            }
            else
            {
                var name = m.Groups[2].Value;
                var width = m.Groups[3].Success ? int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : 12;
                if (cx.Values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    var span = t.Span(" " + value + " ").Underline().SemiBold();
                    if (size is { } fs) { span.FontSize(fs); }
                }
                else
                {
                    var span = t.Span(new string(m.Groups[4].Success ? '.' : '_', width));
                    if (bold) { span.Bold(); }
                    if (size is { } fs2) { span.FontSize(fs2); }
                }
            }

            pos = m.Index + m.Length;
        }

        if (pos < s.Length) { Add(t, s[pos..], bold, size); }
    }

    private static void Add(TextDescriptor t, string text, bool bold, float? size)
    {
        var span = t.Span(text);
        if (bold) { span.Bold(); }
        if (size is { } fs) { span.FontSize(fs); }
    }

    private static string Value(Ctx cx, string key) => cx.Values.GetValueOrDefault(key) ?? string.Empty;

    // ---------------------------------------------------------------- first schedule

    private static void ScheduleRow(ColumnDescriptor col, FormLine l, Ctx cx)
    {
        if (l.Kind == "ROWH")
        {
            col.Item().PaddingTop(cx.Sp(8)).Row(r =>
            {
                r.ConstantItem(26).Text(l.Part(0));
                r.RelativeItem().Text(l.Part(1));
            });
            return;
        }

        var valueText = l.Part(2);
        var label = l.Part(1);
        var underlineLabel = label == "__";
        col.Item().MinHeight(cx.Sp(26f)).Row(r =>
        {
            r.ConstantItem(26).AlignBottom().Text(l.Part(0));
            if (underlineLabel) { r.ConstantItem(220).AlignBottom().PaddingRight(10).BorderBottom(0.6f).Text(" "); }
            else { r.ConstantItem(220).AlignBottom().Text(t => Inline(t, label, cx)); }
            r.ConstantItem(14).AlignBottom().Text(l.Part(0).Length == 0 && label.Length == 0 ? " " : ":");
            r.RelativeItem().AlignBottom().PaddingLeft(4).BorderBottom(0.6f).Text(t => InlineValue(t, valueText, cx));
            if (l.Part(3).Length > 0) { r.ConstantItem(48).AlignBottom().PaddingLeft(3).Text(l.Part(3)); }
        });
    }

    /// <summary>A filled value sits on the rule; an unknown one leaves the rule empty.</summary>
    private static void InlineValue(TextDescriptor t, string s, Ctx cx)
    {
        foreach (Match m in Token.Matches(s))
        {
            if (m.Groups[2].Success && cx.Values.TryGetValue(m.Groups[2].Value, out var v) && !string.IsNullOrWhiteSpace(v))
            {
                t.Span(s[..m.Index]);
                t.Span(v).SemiBold();
                return;
            }
        }

        t.Span(" ");
    }

    // ---------------------------------------------------------------- second schedule

    private static void Schedule2(ColumnDescriptor col, FormPage form, Ctx cx)
    {
        col.Item().Height(cx.Sp(36));
        foreach (var l in form.Body.Where(l => l.Kind != "SCHEDULE2" && l.Kind != "SP" && l.Kind != "LB")) { Line(col, l, cx, 30); }

        var schedule = cx.Data.Schedule;
        var groups = new List<(int Count, decimal Emi, decimal Total)>();
        foreach (var row in schedule)
        {
            var emi = Math.Round(row.Emi, 0, MidpointRounding.AwayFromZero);
            if (groups.Count > 0 && groups[^1].Emi == emi) { groups[^1] = (groups[^1].Count + 1, emi, groups[^1].Total + row.Emi); }
            else { groups.Add((1, emi, row.Emi)); }
        }

        for (var g = 0; g < groups.Count; g++) { groups[g] = groups[g] with { Total = Math.Round(groups[g].Total, 0, MidpointRounding.AwayFromZero) }; }

        var indian = CultureInfo.GetCultureInfo("en-IN");
        for (var i = 0; i < 4; i++)
        {
            var known = groups.Count <= 4 && i < groups.Count;
            string N() => known ? " " + groups[i].Count.ToString(CultureInfo.InvariantCulture) + " " : new string('.', 26);
            string A() => known ? " " + groups[i].Emi.ToString("N0", indian) + " " : new string('.', 26);
            string T() => known ? " " + groups[i].Total.ToString("N0", indian) + " " : new string('.', 34);
            col.Item().PaddingBottom(cx.Sp(7)).Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(cx.Fs(10.2f)));
                t.Span("No. of Instalments");
                Dots(t, N(), known);
                t.Span(" Amount Rs");
                Dots(t, A(), known);
                t.Span(" Total amount Rs");
                Dots(t, T(), known);
            });
        }

        col.Item().Row(r =>
        {
            r.RelativeItem();
            r.ConstantItem(150).AlignRight().AlignMiddle().PaddingRight(4).Text("Total Amount Due Rs.");
            r.ConstantItem(105).Height(cx.Sp(34)).Border(0.7f).AlignMiddle().PaddingLeft(5)
                .Text(schedule.Count > 0 ? Math.Round(schedule.Sum(x => x.Emi), 0, MidpointRounding.AwayFromZero).ToString("N0", indian) : string.Empty).SemiBold();
        });
        col.Item().Height(cx.Sp(8));

        col.Item().BorderTop(1f).Row(r =>
        {
            for (var side = 0; side < 2; side++)
            {
                r.RelativeItem().Row(h =>
                {
                    h.ConstantItem(30).Text("S.No.").FontSize(cx.Fs(9f));
                    h.RelativeItem().AlignCenter().Text("Due Date").FontSize(cx.Fs(9f));
                    h.RelativeItem().AlignCenter().Text("Due Amount").FontSize(cx.Fs(9f));
                });
                if (side == 0) { r.ConstantItem(16); }
            }
        });
        col.Item().BorderTop(1f).Height(cx.Sp(2));

        for (var i = 0; i < 24; i++)
        {
            var left = i;
            col.Item().Height(cx.Sp(19.6f)).Row(r =>
            {
                ScheduleCell(r, left, schedule, indian, cx);
                r.ConstantItem(16);
                ScheduleCell(r, left + 24, schedule, indian, cx);
            });
        }

        col.Item().Height(cx.Sp(6));
        col.Item().BorderTop(1.4f).PaddingTop(cx.Sp(6)).Text(form.Body.First(l => l.Kind == "LB").Text).Bold().FontSize(cx.Fs(10.4f));
    }

    private static void Dots(TextDescriptor t, string text, bool filled)
    {
        var span = t.Span(text);
        if (filled) { span.Underline().SemiBold(); }
    }

    private static void ScheduleCell(RowDescriptor r, int index, IReadOnlyList<AmortizationRow> schedule, CultureInfo indian, Ctx cx)
    {
        r.RelativeItem().Row(c =>
        {
            c.ConstantItem(26).AlignBottom().Text((index + 1).ToString(CultureInfo.InvariantCulture) + ".");
            var row = index < schedule.Count ? schedule[index] : null;
            c.RelativeItem().PaddingHorizontal(4).AlignBottom().BorderBottom(0.5f).AlignCenter()
                .Text(row?.DueDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? " ");
            c.ConstantItem(18).AlignBottom().Text("Rs");
            c.RelativeItem().AlignBottom().BorderBottom(0.5f).AlignCenter().Text(row is null ? " " : row.Emi.ToString("N0", indian));
        });
    }

    // ---------------------------------------------------------------- proposal form

    private static void ProposalRow(ColumnDescriptor col, FormLine l, Ctx cx)
    {
        var label = l.Part(0);
        var h = cx.Sp(28f);
        if (label.Length == 0 && l.Part(1).Length == 0)
        {
            col.Item().Height(cx.Sp(13f));
            return;
        }

        var split = l.Part(2) == "Date of Birth:" ? 7f : 4f;
        col.Item().MinHeight(h).Row(r =>
        {
            r.RelativeItem(split).Text(t => { t.Span(label); PlainValue(t, l.Part(1), cx); });
            if (l.Part(2).Length > 0)
            {
                r.RelativeItem(10 - split).Text(t => { t.Span(l.Part(2)); PlainValue(t, l.Part(3), cx); });
            }
        });
    }

    private static void PlainValue(TextDescriptor t, string field, Ctx cx)
    {
        var m = Token.Match(field);
        if (m.Success && m.Groups[2].Success && cx.Values.TryGetValue(m.Groups[2].Value, out var v) && !string.IsNullOrWhiteSpace(v))
        {
            t.Span(" " + v).SemiBold();
        }
    }

    private static void ProposalChoice(ColumnDescriptor col, FormLine l, Ctx cx)
    {
        var choice = Token.Match(l.Part(4)) is { Success: true } m && cx.Values.TryGetValue(m.Groups[2].Value, out var v) ? v : null;
        col.Item().MinHeight(cx.Sp(28f)).Row(r =>
        {
            r.RelativeItem(36).Text(l.Part(0));
            foreach (var (opt, weight) in new[] { (l.Part(1), 15f), (l.Part(2), 24f), (l.Part(3), 20f) })
            {
                r.RelativeItem(weight).Text(t =>
                {
                    var span = t.Span(opt);
                    if (opt.Length > 0 && string.Equals(opt, choice, StringComparison.OrdinalIgnoreCase)) { span.Bold().Underline(); }
                });
            }
        });
    }

    // ---------------------------------------------------------------- arbitration vehicle schedule

    private static void ArbitrationSchedule(ColumnDescriptor col, Ctx cx)
    {
        col.Item().PaddingTop(cx.Sp(8)).Border(0.8f).Column(c =>
        {
            c.Item().AlignCenter().BorderBottom(0.6f).Text("SCHEDULE").FontSize(cx.Fs(9f));
            c.Item().Table(t =>
            {
                t.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(2.2f); cols.RelativeColumn(1.3f); cols.RelativeColumn(1f); cols.RelativeColumn(1.2f);
                    cols.RelativeColumn(1.4f); cols.RelativeColumn(1.4f); cols.RelativeColumn(3f);
                });
                foreach (var h in new[] { "Make of Vehicle", "Model", "HP/CC", "Type", "Chassis No.", "Engine No.", "Registration Mark & No." })
                {
                    t.Cell().Border(0.5f).Padding(2).AlignCenter().Text(h).FontSize(cx.Fs(8.2f));
                }

                foreach (var key in new[] { "make", "model", "hp_cc", "vehicle_type", "chassis_no", "engine_no", "reg_no" })
                {
                    t.Cell().Border(0.5f).Height(cx.Sp(42)).Padding(2).AlignCenter().AlignMiddle().Text(Value(cx, key)).FontSize(cx.Fs(8.2f)).SemiBold();
                }
            });
        });
    }
}

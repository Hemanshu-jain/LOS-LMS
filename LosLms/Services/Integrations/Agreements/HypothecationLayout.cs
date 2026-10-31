namespace LosLms.Services.Agreements;

/// <summary>
/// Fixed geometry of the printed form (PDF points, origin bottom-left, 1-based pages as in the scan): the page size, the footer blocks, and where
/// each signer signs. The form is signed at the foot of nearly every page (LENDER / BORROWER), plus the blocks on the proposal, power of attorney,
/// guarantee and arbitration pages, so a borrower has many boxes; the e-Sign service stamps every one.
/// </summary>
public static class HypothecationLayout
{
    public const float PageW = 595.28f;
    public const float PageH = 936f;
    public const float MarginX = 46f;
    public const float MarginTop = 34f;
    public const float MarginBottom = 20f;

    public const float StdFooterH = 48f;
    public const float LabelRowH = 16f;
    public const float ArbFooterH = 50f;
    public const float P16Bottom = 100f;
    public const float P16FooterH = 44f + 16f + 56f + 84f + P16Bottom;
    public const float P18BlockH = 215f;
    public const float P18FooterH = P18BlockH + StdFooterH;
    public const float P20BlockH = 215f;
    public const float P20FooterH = P20BlockH + StdFooterH;

    private const float BoxH = 28f;

    /// <summary>Pages whose foot carries the standard LENDER / BORROWER row: the agreement itself, both schedules, the power of attorney and the guarantee.</summary>
    private static readonly int[] StdFootPages = Enumerable.Range(3, 12).Concat(new[] { 17, 19 }).ToArray();

    /// <summary>Where a party signs, as Digio boxes. Applicant first; a co-applicant signs beside the applicant; the guarantor on the guarantor blocks.</summary>
    public static IReadOnlyList<SignBox>? BoxesFor(string partyType)
    {
        var boxes = new List<SignBox>();
        switch (partyType)
        {
            case "Applicant":
            case "CoApplicant":
            {
                var co = partyType == "CoApplicant";
                var stdUrx = PageW - MarginX - 14 - (co ? 110 : 0);
                boxes.AddRange(StdFootPages.Select(p => Box(p, stdUrx - 100, MarginBottom + LabelRowH + 4, stdUrx, BoxH)));

                // 16 proposal form: "Signature of Borrower" (right)
                var p16Urx = PageW - MarginX - (co ? 140 : 0);
                boxes.Add(Box(16, p16Urx - 130, MarginBottom + P16FooterH - 2 - 34, p16Urx, 34));

                // 18 power of attorney: "SIGNED AND DELIVERED by the within named"
                var p18Llx = MarginX + 110 + (co ? 120 : 0);
                boxes.Add(Box(18, p18Llx, MarginBottom + P18FooterH - 18 - 2 - 36, p18Llx + 110, 36));

                // 21 arbitration: above BORROWER (centre)
                var p21Llx = PageW / 2 - 50 - (co ? 110 : 0);
                boxes.Add(Box(21, p21Llx, MarginBottom + LabelRowH + 6, p21Llx + 100, BoxH));
                break;
            }

            case "Guarantor":
                boxes.Add(Box(16, MarginX, MarginBottom + P16FooterH - 2 - 34, MarginX + 130, 34));
                boxes.Add(Box(20, PageW - MarginX - 150, MarginBottom + P20FooterH - 32 - 10 - 2 - 30, PageW - MarginX - 20, 30));
                boxes.Add(Box(21, PageW - MarginX - 100, MarginBottom + LabelRowH + 6, PageW - MarginX, BoxH));
                break;
            default:
                return null;
        }

        return boxes;
    }

    /// <summary>Where the lender's own signature image is stamped (not an e-Sign signer): above LENDER / FINANCIER at the foot.</summary>
    public static SignBox? LenderStamp(int page) =>
        StdFootPages.Contains(page) || page is 18 or 20
            ? Box(page, MarginX + 14, MarginBottom + LabelRowH + 4, MarginX + 114, BoxH)
            : page == 21 ? Box(page, MarginX, MarginBottom + LabelRowH + 6, MarginX + 100, BoxH) : null;

    private static SignBox Box(int page, double llx, double lly, double urx, double height) =>
        new(page, Math.Round(llx, 1), Math.Round(lly, 1), Math.Round(urx, 1), Math.Round(lly + height, 1));
}

namespace LosLms.Services;

/// <summary>A signature box on the agreement PDF in Digio's coordinates (points; page numbers start at 1).</summary>
public sealed record SignBox(int Page, double Llx, double Lly, double Urx, double Ury);

/// <summary>
/// Where each borrower signs on the generated agreement. The agreement builder (LoanAgreementPdf) supplies the real layout; with none registered
/// (or no box for a party) the agreement goes out with Digio's own placement and no coordinate charge.
/// </summary>
public interface IAgreementSignatureBoxes
{
    /// <summary>The boxes for a party type (Applicant, CoApplicant, Guarantor), or null when that party has none.</summary>
    IReadOnlyList<SignBox>? BoxesFor(string partyType);

    /// <summary>The boxes for the agreement template the company issues (<see cref="LosLms.Models.AgreementTemplates"/>); implementations that do not tell templates apart keep the default.</summary>
    IReadOnlyList<SignBox>? BoxesFor(string partyType, string? template) => BoxesFor(partyType);
}

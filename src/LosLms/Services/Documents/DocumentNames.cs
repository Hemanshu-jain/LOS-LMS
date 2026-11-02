using System.Text;

namespace LosLms.Services;

/// <summary>
/// One naming rule for every generated document: <c>&lt;ApplicantName&gt;_&lt;ApplicationDate yyyy-MM-dd&gt;_&lt;ApplicationNo&gt;_&lt;DocType&gt;.pdf</c>,
/// e.g. <c>Hemanshu-Jain_2026-10-16_KIS-2026-000010_Loan-Agreement.pdf</c>. Safe characters only (letters, digits, hyphen).
/// </summary>
public static class DocumentNames
{
    public const string LoanAgreement = "Loan-Agreement";
    public const string LoanAgreementSigned = "Loan-Agreement-SIGNED";
    public const string WelcomeLetter = "Welcome-Letter";
    public const string DisbursementMemo = "Disbursement-Memo";
    public const string Cam = "CAM";

    public static string For(string applicationNo, string? applicantName, DateTime applicationDate, string docType) =>
        $"{Safe(applicantName, "Customer")}_{applicationDate:yyyy-MM-dd}_{Safe(applicationNo, "Application")}_{Safe(docType, "Document")}.pdf";

    /// <summary>Letters and digits kept, every other run collapsed to a single hyphen, ends trimmed; <paramref name="fallback"/> when nothing is left.</summary>
    public static string Safe(string? text, string fallback)
    {
        var sb = new StringBuilder();
        foreach (var c in (text ?? string.Empty).Trim())
        {
            if (char.IsAsciiLetterOrDigit(c)) { sb.Append(c); }
            else if (sb.Length > 0 && sb[^1] != '-') { sb.Append('-'); }
        }

        var cleaned = sb.ToString().Trim('-');
        return cleaned.Length > 0 ? cleaned : fallback;
    }
}

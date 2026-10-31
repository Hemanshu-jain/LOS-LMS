using System.Globalization;
using LosLms.Models;

namespace LosLms.Services.Agreements;

/// <summary>Whole rupees in words, Indian system ("Fourteen Lakh Twenty Five Thousand Only").</summary>
public static class NumberWords
{
    private static readonly string[] Small =
    {
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen",
        "Sixteen", "Seventeen", "Eighteen", "Nineteen",
    };

    private static readonly string[] Tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

    public static string Rupees(decimal amount)
    {
        var n = (long)Math.Round(Math.Abs(amount), MidpointRounding.AwayFromZero);
        if (n == 0) { return "Zero Only"; }

        var parts = new List<string>();
        void Part(long value, string unit)
        {
            if (value > 0) { parts.Add(Below1000(value) + (unit.Length > 0 ? " " + unit : "")); }
        }

        Part(n / 10_000_000, "Crore");
        n %= 10_000_000;
        Part(n / 100_000, "Lakh");
        n %= 100_000;
        Part(n / 1_000, "Thousand");
        n %= 1_000;
        Part(n, "");
        return string.Join(" ", parts) + " Only";
    }

    private static string Below1000(long n)
    {
        var s = new List<string>();
        if (n >= 100) { s.Add(Small[n / 100] + " Hundred"); n %= 100; }
        if (n >= 20) { s.Add(Tens[n / 10] + (n % 10 > 0 ? " " + Small[n % 10] : "")); }
        else if (n > 0) { s.Add(Small[n]); }
        return string.Join(" ", s);
    }
}

/// <summary>
/// The values the printed hypothecation form is filled with, taken from the application. A key that is missing (or blank) means the app does not
/// hold that fact, and the form keeps its printed blank line.
/// </summary>
public static class HypothecationValues
{
    private static readonly CultureInfo Indian = CultureInfo.GetCultureInfo("en-IN");

    public static Dictionary<string, string> Build(AgreementData d)
    {
        var v = new Dictionary<string, string>(StringComparer.Ordinal);
        void Set(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) { v[key] = value.Trim(); }
        }

        // Spreads one text over consecutive printed blank lines: keys[i] receives what fits in widths[i]; the last line takes the rest.
        void SetLines(string? text, string[] keys, int[] widths)
        {
            var rest = text?.Trim() ?? string.Empty;
            for (var i = 0; i < keys.Length && rest.Length > 0; i++)
            {
                var (line, remainder) = i == keys.Length - 1 ? (rest, string.Empty) : Cut(rest, widths[i]);
                Set(keys[i], line);
                rest = remainder;
            }
        }

        var date = d.ExecutionDate;
        var lender = d.Lender;
        var app = d.Application;
        var applicant = d.Parties.FirstOrDefault(p => p.PartyType == "Applicant");
        var coApplicants = d.Parties.Where(p => p.PartyType == "CoApplicant" && !string.IsNullOrWhiteSpace(p.FullName)).ToList();
        var guarantors = d.Parties.Where(p => p.PartyType == "Guarantor" && !string.IsNullOrWhiteSpace(p.FullName)).ToList();
        var borrowers = new[] { applicant }.Concat(coApplicants).Where(p => p is not null && !string.IsNullOrWhiteSpace(p!.FullName)).Select(p => p!).ToList();

        // ---- execution
        Set("day", date.Day.ToString(CultureInfo.InvariantCulture));
        Set("month", date.ToString("MMMM", CultureInfo.InvariantCulture));
        Set("yy", (date.Year % 100).ToString("00", CultureInfo.InvariantCulture));
        Set("agreement_date", date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        Set("agreement_no", app.Id);
        Set("place", lender.RegisteredCity);
        Set("arb_venue", lender.RegisteredCity);

        // ---- lender
        var lenderName = lender.Name;
        Set("lender_name", lenderName);
        SetLines(lender.Address, new[] { "lender_address", "lender_address_2", "lender_address_3" }, new[] { 100, 150, 150 });
        var lenderWithAddress = Join(", ", lenderName, Blank(lender.Address) is { } a ? "having its registered office at " + a : null);
        Set("lender_line", lenderWithAddress);
        SetLines(lenderWithAddress, new[] { "poa_lender_1", "poa_lender_2" }, new[] { 100, 140 });

        // ---- borrowers
        var borrowerNames = string.Join(" and ", borrowers.Select(b => b.FullName!.Trim()));
        SetLines(borrowerNames, new[] { "borrower_names_1", "borrower_names_2" }, new[] { 70, 140 });
        SetLines(string.Join(" and ", borrowers.Select(BorrowerSentence)), new[] { "borrower_name_1", "borrower_name_2" }, new[] { 70, 140 });
        SetLines(borrowers.Count > 0 ? Address(borrowers[0]) : null, new[] { "borrower_address_1", "borrower_address_2" }, new[] { 150, 150 });

        SetLines(string.Join(" and ", guarantors.Select(g => g.FullName!.Trim())), new[] { "guarantor_names_1", "guarantor_names_2" }, new[] { 70, 140 });
        Set("guarantor_name", guarantors.ElementAtOrDefault(0)?.FullName);
        Set("guarantor_name_2", guarantors.ElementAtOrDefault(1)?.FullName);

        // ---- loan
        var sanctioned = d.Decision.SanctionedAmount ?? 0m;
        if (sanctioned > 0)
        {
            Set("loan_amount", sanctioned.ToString("N0", Indian));
            Set("amount_words", NumberWords.Rupees(sanctioned));
        }

        if (d.Decision.SanctionedTenure is { } months and > 0) { Set("loan_months", months.ToString(CultureInfo.InvariantCulture)); }
        if (d.Schedule.Count > 0) { Set("instalments", d.Schedule.Count.ToString(CultureInfo.InvariantCulture)); }
        if (d.Decision.SanctionedRoi is { } roi) { Set("roi", roi.ToString("0.##", CultureInfo.InvariantCulture) + "% p.a. (fixed)"); }
        if (app.AdvanceEmi is { } adv and > 0) { Set("advance_emi", adv.ToString("N0", Indian)); }
        if (d.BounceCharge is { } bounce and > 0)
        {
            Set("bounce_charge", "Rs " + (bounce + Math.Round(bounce * d.BounceGstPct / 100m, 2)).ToString("N0", Indian) + " per instance (incl. GST)");
        }

        // ---- the asset
        var s = d.Security;
        if (s is not null)
        {
            if (string.Equals(s.AssetType, "Property", StringComparison.OrdinalIgnoreCase))
            {
                var property = Join(", ", "Immovable property", s.PropertyType, s.PropertyAddress);
                SetLines(property, new[] { "asset_description", "asset_description_2" }, new[] { 70, 70 });
                SetLines(property, new[] { "asset_line_1", "asset_line_2" }, new[] { 150, 150 });
            }
            else
            {
                var (make, model) = SplitMakeModel(s.MakeModel, d.VehicleCatalog);
                Set("make", make);
                Set("model", model);
                Set("engine_no", s.EngineNo);
                Set("chassis_no", s.ChassisNo);
                Set("reg_no", s.RegNo);
                var desc = Join(", ", "Vehicle", s.MakeModel, Blank(s.MfgYear) is { } y ? "year " + y : null, Blank(s.RegNo) is { } r ? "Reg. No. " + r : null);
                SetLines(desc, new[] { "asset_description", "asset_description_2" }, new[] { 70, 70 });
                SetLines(Join(", ", s.MakeModel, Blank(s.MfgYear) is { } y2 ? "year " + y2 : null, Blank(s.RegNo) is { } r2 ? "Reg. No. " + r2 : null,
                    Blank(s.ChassisNo) is { } c ? "Chassis No. " + c : null, Blank(s.EngineNo) is { } e ? "Engine No. " + e : null),
                    new[] { "asset_line_1", "asset_line_2" }, new[] { 150, 150 });
            }
        }

        // ---- arbitration (per-company setting)
        SetLines(lender.Arbitrator1, new[] { "arbitrator_1", "arbitrator_1_b", "arbitrator_1_c" }, new[] { 90, 100, 100 });
        SetLines(lender.Arbitrator2, new[] { "arbitrator_2", "arbitrator_2_b", "arbitrator_2_c" }, new[] { 90, 100, 100 });
        if (lender.ArbitrationFee is { } fee and > 0)
        {
            Set("arbitration_fee", fee.ToString("N0", Indian));
            Set("arbitration_fee_words", NumberWords.Rupees(fee));
        }

        // ---- proposal form: borrower
        if (applicant is not null)
        {
            Set("borrower_name", applicant.FullName);
            Set("poa_borrower_name", string.Join(" and ", borrowers.Select(b => b.FullName!.Trim())));
            Set("dob", applicant.DateOfBirth?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
            Set("father_name", applicant.FatherSpouseName);
            Set("poa_father", applicant.FatherSpouseName);
            Set("address_registration", Address(applicant, withPin: false));
            Set("address_residential", Address(applicant, withPin: false));
            Set("poa_address", Address(applicant));
            Set("pin_registration", applicant.PinCode);
            Set("pin_residential", applicant.PinCode);
            Set("mobile1", applicant.Mobile);
            Set("mobile2", applicant.AltMobile);
            Set("years_at_address", applicant.YearsAtAddress is { } yrs ? yrs.ToString(CultureInfo.InvariantCulture) + " years" : null);
            Set("email", applicant.Email);
            Set("office_address", applicant.OfficeAddress);
            Set("pan", LoanAgreementPdf.MaskPan(applicant.Pan) is var pan && pan != "—" ? pan : null);
            Set("aadhaar", LoanAgreementPdf.MaskAadhaar(applicant.Aadhaar) is var aad && aad != "—" ? aad : null);
            Set("occupation", Join(", ", applicant.EmploymentType, applicant.EmployerName, applicant.Designation));
            Set("residence_choice", ResidenceChoice(applicant.ResidenceType));
        }

        var refs = d.References ?? Array.Empty<LosLms.Models.Reference>();
        Set("reference_1", ReferenceLine(refs.ElementAtOrDefault(0)));
        Set("reference_2", ReferenceLine(refs.ElementAtOrDefault(1)));
        Set("introduced_by", app.SourcingChannel);

        // ---- proposal form: guarantor
        if (guarantors.FirstOrDefault() is { } g)
        {
            Set("guarantor_dob", g.DateOfBirth?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
            Set("guarantor_father", g.FatherSpouseName);
            Set("guarantor_address", Address(g, withPin: false));
            Set("guarantor_pin", g.PinCode);
            Set("guarantor_pin2", g.PinCode);
            Set("guarantor_years", g.YearsAtAddress is { } gy ? gy.ToString(CultureInfo.InvariantCulture) + " years" : null);
            Set("guarantor_mobile1", g.Mobile);
            Set("guarantor_mobile2", g.AltMobile);
            Set("guarantor_residence_choice", ResidenceChoice(g.ResidenceType));
        }

        return v;
    }

    /// <summary>Cuts text at a word boundary at or before <paramref name="width"/> characters.</summary>
    internal static (string Line, string Remainder) Cut(string text, int width)
    {
        text = text.Trim();
        if (text.Length <= width) { return (text, string.Empty); }

        var at = text.LastIndexOf(' ', width);
        if (at <= 0) { at = width; }
        return (text[..at].TrimEnd(), text[at..].TrimStart());
    }

    private static string BorrowerSentence(Party p)
    {
        var parts = new List<string> { p.FullName!.Trim() };
        if (Blank(p.FatherSpouseName) is { } f) { parts.Add("S/D/W of " + f); }
        if (Address(p) is { Length: > 0 } a) { parts.Add("residing at " + a); }
        return string.Join(", ", parts);
    }

    private static string Address(Party p, bool withPin = true) =>
        Join(", ", Blank(p.Address1), Blank(p.Address2), Blank(p.City), Join(" ", Blank(p.State), withPin ? Blank(p.PinCode) : null));

    private static string? ResidenceChoice(string? type) => type?.Trim().ToLowerInvariant() switch
    {
        "owned" => "Owned",
        "rented" => "Rented",
        _ => null,
    };

    private static string? ReferenceLine(LosLms.Models.Reference? r) =>
        r is null ? null : Blank(Join(", ", Blank(r.Name), Blank(r.Relationship), Blank(r.Mobile), Blank(r.Address)));

    private static (string? Make, string? Model) SplitMakeModel(string? makeModel, IReadOnlyList<VehicleLoanCap>? catalog)
    {
        var text = makeModel?.Trim();
        if (string.IsNullOrEmpty(text)) { return (null, null); }

        var hit = catalog?.FirstOrDefault(c => string.Equals($"{c.Make} {c.Model}", text, StringComparison.OrdinalIgnoreCase));
        if (hit is not null) { return (hit.Make, hit.Model); }

        var space = text.IndexOf(' ');
        return space < 0 ? (text, null) : (text[..space], text[(space + 1)..]);
    }

    internal static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string Join(string sep, params string?[] parts) => string.Join(sep, parts.Where(p => !string.IsNullOrWhiteSpace(p)));
}

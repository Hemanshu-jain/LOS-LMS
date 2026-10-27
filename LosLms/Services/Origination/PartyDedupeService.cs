using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>An earlier application of the same person, in the same company, that is no longer open.</summary>
public sealed record EarlierApplication(string ApplicationId, string Status, DateTime CreatedAt, Party Party);

/// <param name="Status">"Pass" or "Fail" (the value stored on <see cref="Party.DedupeStatus"/>).</param>
/// <param name="Reason">Why it failed; null on Pass.</param>
/// <param name="Earlier">Closed (Sanctioned/Rejected) applications of the same person, newest first.</param>
/// <param name="MobileWarning">Another application shares this mobile but no PAN/Aadhaar: shown, never blocking.</param>
public sealed record DedupeResult(string Status, string? Reason, IReadOnlyList<EarlierApplication> Earlier, bool MobileWarning);

/// <summary>
/// The dedupe rule for a party, in one place (every identifier comparison lives here, so changing how Aadhaar is stored
/// or matched touches only <see cref="Matches"/>).
/// </summary>
/// <remarks>
/// Scope is always the party's own company, whoever is looking (a SuperAdmin included): another company's files are never
/// compared, shown or counted. Per other party Q on another application B, same company:
/// PAN or Aadhaar equal and B open (New / In progress) = Fail; equal and B Sanctioned/Rejected = Pass, "returning";
/// mobile equal only = Pass with a warning. On the SAME application a shared PAN/Aadhaar still fails (one person entered
/// twice) but a shared mobile is ignored (a spouse).
/// </remarks>
public static class PartyDedupeService
{
    public const string Pass = "Pass";
    public const string Fail = "Fail";

    public static bool IsOpen(string status) => status is "New" or "In progress";

    /// <param name="statusOverride">An application status that has been set but not saved yet (the sanction/reject paths).</param>
    public static async Task<DedupeResult> CheckAsync(
        LosDbContext db, Party party, (string ApplicationId, string Status)? statusOverride = null)
    {
        var pan = Blank(party.Pan);
        var aadhaar = Blank(party.Aadhaar);
        var mobile = Blank(party.Mobile);
        var none = new DedupeResult(Pass, null, Array.Empty<EarlierApplication>(), false);
        if (pan is null && aadhaar is null && mobile is null)
        {
            return none;
        }

        var companyId = await db.Applications.IgnoreQueryFilters()
            .Where(a => a.Id == party.ApplicationId).Select(a => (int?)a.CompanyId).FirstOrDefaultAsync();
        if (companyId is null)
        {
            return none;
        }

        var rows = await db.Parties.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.Id != party.Id && p.Application!.CompanyId == companyId
                && ((pan != null && p.Pan == pan) || (aadhaar != null && p.Aadhaar == aadhaar) || (mobile != null && p.Mobile == mobile)))
            .Select(p => new { Party = p, p.Application!.Status, p.Application.CreatedAt })
            .ToListAsync();

        var earlier = new Dictionary<string, EarlierApplication>();
        string? reason = null;
        var mobileWarning = false;
        foreach (var r in rows.OrderByDescending(r => r.CreatedAt))
        {
            var sameApplication = r.Party.ApplicationId == party.ApplicationId;
            var status = statusOverride is { } o && o.ApplicationId == r.Party.ApplicationId ? o.Status : r.Status;

            if (Matches(party, r.Party))
            {
                if (sameApplication)
                {
                    reason ??= "The same PAN or Aadhaar is entered twice on this application.";
                }
                else if (IsOpen(status))
                {
                    reason ??= "This person already has another open application in your company.";
                }
                else
                {
                    earlier.TryAdd(r.Party.ApplicationId, new EarlierApplication(r.Party.ApplicationId, status, r.CreatedAt, r.Party));
                }
            }
            else if (!sameApplication)
            {
                mobileWarning = true;
            }
        }

        return new DedupeResult(reason is null ? Pass : Fail, reason, earlier.Values.ToList(), mobileWarning);
    }

    /// <summary>Same person for blocking purposes: PAN or Aadhaar equal (mobile alone is only a warning).</summary>
    private static bool Matches(Party a, Party b) =>
        (Blank(a.Pan) is { } pan && pan == Blank(b.Pan)) || (Blank(a.Aadhaar) is { } aadhaar && aadhaar == Blank(b.Aadhaar));

    /// <summary>
    /// Re-evaluates every party that shares any of <paramref name="identifiers"/>, so both sides of a match agree.
    /// Changed rows are left tracked for the caller's SaveChanges.
    /// </summary>
    public static async Task RecomputeAsync(
        LosDbContext db, IReadOnlyCollection<string> identifiers, IReadOnlySet<int> alreadyDone,
        (string ApplicationId, string Status)? statusOverride = null)
    {
        if (identifiers.Count == 0)
        {
            return;
        }

        var affected = await db.Parties
            .Where(p => (p.Pan != null && identifiers.Contains(p.Pan))
                || (p.Aadhaar != null && identifiers.Contains(p.Aadhaar))
                || (p.Mobile != null && identifiers.Contains(p.Mobile)))
            .ToListAsync();

        foreach (var party in affected.Where(p => !alreadyDone.Contains(p.Id)))
        {
            party.DedupeStatus = (await CheckAsync(db, party, statusOverride)).Status;
        }
    }

    /// <summary>
    /// An application's status is about to change: re-evaluate the parties on OTHER applications that share an identifier
    /// with it, using the new status. Called before the caller's SaveChanges, which persists the result.
    /// </summary>
    public static async Task RecomputeForStatusChangeAsync(LosDbContext db, string applicationId, string newStatus)
    {
        var mine = await db.Parties.AsNoTracking().Where(p => p.ApplicationId == applicationId).ToListAsync();
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in mine)
        {
            AddIdentifiers(identifiers, p.Pan, p.Aadhaar, p.Mobile);
        }

        var ownIds = mine.Select(p => p.Id).ToHashSet();
        await RecomputeAsync(db, identifiers, ownIds, (applicationId, newStatus));
    }

    public static void AddIdentifiers(HashSet<string> set, params string?[] values)
    {
        foreach (var value in values)
        {
            if (Blank(value) is { } identifier)
            {
                set.Add(identifier);
            }
        }
    }

    /// <summary>
    /// Fills only the BLANK fields of <paramref name="target"/> from an earlier application's party. Identifiers,
    /// verification flags, uploaded files and statuses are never copied.
    /// </summary>
    public static void CopyBlanks(Party source, PartyForm target)
    {
        target.FullName = Fill(target.FullName, source.FullName);
        target.DateOfBirth ??= source.DateOfBirth;
        target.Gender = Fill(target.Gender, source.Gender);
        target.MaritalStatus = Fill(target.MaritalStatus, source.MaritalStatus);
        target.FatherSpouseName = Fill(target.FatherSpouseName, source.FatherSpouseName);
        target.CustomerCategory = Fill(target.CustomerCategory, source.CustomerCategory);
        target.Nationality = Fill(target.Nationality, source.Nationality);
        target.MotherTongue = Fill(target.MotherTongue, source.MotherTongue);
        target.AltMobile = Fill(target.AltMobile, source.AltMobile);
        target.Email = Fill(target.Email, source.Email);
        target.Address1 = Fill(target.Address1, source.Address1);
        target.Address2 = Fill(target.Address2, source.Address2);
        target.City = Fill(target.City, source.City);
        target.State = Fill(target.State, source.State);
        target.PinCode = Fill(target.PinCode, source.PinCode);
        target.ResidenceType = Fill(target.ResidenceType, source.ResidenceType);
        target.YearsAtAddress ??= source.YearsAtAddress;
        target.EmploymentType = Fill(target.EmploymentType, source.EmploymentType);
        target.EmployerName = Fill(target.EmployerName, source.EmployerName);
        target.Designation = Fill(target.Designation, source.Designation);
        target.MonthlyIncome ??= source.MonthlyIncome;
        target.YearsInJob ??= source.YearsInJob;
        target.OfficeAddress = Fill(target.OfficeAddress, source.OfficeAddress);
    }

    private static string? Fill(string? current, string? from) => string.IsNullOrWhiteSpace(current) ? from : current;

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>How an Aadhaar number is shown. The full number stays stored; this is display only.</summary>
public static class AadhaarMask
{
    /// <summary>"XXXX XXXX 1234" for a 12-digit number; anything else (blank, or half typed) is returned as is.</summary>
    public static string Of(string? aadhaar)
    {
        var value = aadhaar?.Trim();
        return value is { Length: 12 } && value.All(char.IsAsciiDigit) ? "XXXX XXXX " + value[^4..] : value ?? string.Empty;
    }
}

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// The only code that moves a loan's payout through Draft → PendingApproval → Approved → Released. The money itself is paid from
/// the NBFC's own bank, outside this app; this makes sure it is paid only against Admin-approved, frozen details, by someone other
/// than the person who prepared them, and that the file records exactly what was paid, by whom and when.
/// </summary>
/// <remarks>
/// Every rule is enforced here, on the server, against what is in the database. The Post Sanction page only shows the state and
/// calls these methods, so a stale page, a crafted event or the 8-second auto-save cannot get round them.
/// </remarks>
public static partial class DisbursementWorkflow
{
    public const string Draft = "Draft";
    public const string PendingApproval = "PendingApproval";
    public const string Approved = "Approved";
    public const string Released = "Released";

    /// <summary>Evidence subject for the bank check of the payee's account (separate from the borrower's own account, BANK_ACCOUNT).</summary>
    public const string PayeeSubject = "PAYEE_ACCOUNT";

    /// <summary>Checklist items owned by a head of department: only an Admin may clear them.</summary>
    public static readonly IReadOnlySet<string> AdminOnlyChecklistItems =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CAM approval", "Rate & PF approval" };

    public static string Label(string status) => status switch
    {
        PendingApproval => "Waiting for Admin approval",
        Approved => "Approved — pay in the bank, then confirm",
        Released => "Released",
        _ => "Being prepared",
    };

    // ---------------------------------------------------------------------
    // Amount and fingerprints
    // ---------------------------------------------------------------------

    /// <summary>What is paid out: the sanctioned amount less every charge not waived (with its GST) and any trade advance already paid.</summary>
    public static decimal NetAmount(decimal sanctionedAmount, IEnumerable<Charge> charges, decimal? tradeAdvance) =>
        sanctionedAmount - charges.Where(c => !c.Waived).Sum(c => c.Amount + c.Gst) - (tradeAdvance ?? 0m);

    /// <summary>Recomputes the payout amount from the database, never from a page.</summary>
    public static async Task<decimal> NetAmountAsync(LosDbContext db, string applicationId, decimal? tradeAdvance)
    {
        var sanctioned = await db.ApprovalDecisions.AsNoTracking().Where(d => d.ApplicationId == applicationId)
            .Select(d => d.SanctionedAmount).FirstOrDefaultAsync() ?? 0m;
        var charges = await db.Charges.AsNoTracking().Where(c => c.ApplicationId == applicationId).ToListAsync();
        return NetAmount(sanctioned, charges, tradeAdvance);
    }

    /// <summary>Identifies one payee account (name + account + IFSC), so a bank check is reused for the same account and redone for another.</summary>
    public static string PayeeFingerprint(string? name, string? account, string? ifsc) =>
        Hash($"{Norm(name)}|{Digits(account)}|{Norm(ifsc)}");

    /// <summary>Everything the Admin approves. Release refuses if any of it changed afterwards.</summary>
    public static string PayoutFingerprint(Disbursement d, decimal amount) =>
        Hash(string.Join('|',
            amount.ToString("0.00", CultureInfo.InvariantCulture), Norm(d.Type), Norm(d.BeneficiaryName), Digits(d.BeneficiaryAccount),
            Norm(d.BeneficiaryIfsc), Norm(d.PaymentMode), Norm(d.DisburseFromAccount),
            // The repayment terms the loan account opens with are part of what is approved, too.
            d.FirstEmiDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "", d.EmiDayOfMonth?.ToString(CultureInfo.InvariantCulture) ?? "",
            d.FirstEmiOverride?.ToString("0.00", CultureInfo.InvariantCulture) ?? "", d.EmiRoundedTo?.ToString(CultureInfo.InvariantCulture) ?? "",
            (d.TradeAdvance ?? 0m).ToString("0.00", CultureInfo.InvariantCulture)));

    private static string Norm(string? s) => Regex.Replace((s ?? string.Empty).Trim().ToUpperInvariant(), @"\s+", " ");

    private static string Digits(string? s) => new((s ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());

    private static string Hash(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));

    // ---------------------------------------------------------------------
    // Validation
    // ---------------------------------------------------------------------

    [GeneratedRegex(@"^[A-Z]{4}0[A-Z0-9]{6}$")]
    private static partial Regex IfscPattern();

    public static bool IsValidIfsc(string? ifsc) => ifsc is not null && IfscPattern().IsMatch(ifsc.Trim().ToUpperInvariant());

    /// <summary>Indian bank account numbers are 9–18 digits (a few banks use letters; those are allowed too).</summary>
    public static bool IsValidAccount(string? account) => Digits(account).Length is >= 9 and <= 18;

    /// <summary>NEFT/RTGS/IMPS references are 12–22 letters and digits; a cheque number is 6 digits.</summary>
    public static bool IsValidReference(string? reference, string? paymentMode)
    {
        var r = (reference ?? string.Empty).Trim();
        return string.Equals(paymentMode, "Cheque", StringComparison.OrdinalIgnoreCase)
            ? Regex.IsMatch(r, @"^\d{6}$")
            : Regex.IsMatch(r, @"^[A-Za-z0-9]{12,22}$");
    }

    /// <summary>Why the payout cannot be submitted for approval yet; empty when it can.</summary>
    public static List<string> MissingForSubmit(Disbursement d, decimal amount, IReadOnlyCollection<PostSanctionChecklist> checklist)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(d.Type)) { missing.Add("Choose the disbursement type."); }
        if (amount <= 0m) { missing.Add("The amount to pay is zero or negative. Check the sanction, charges and trade advance."); }
        if (string.IsNullOrWhiteSpace(d.BeneficiaryName)) { missing.Add("Enter the payee's name exactly as on the bank account."); }
        if (!IsValidAccount(d.BeneficiaryAccount)) { missing.Add("Enter the payee's bank account number (9–18 digits)."); }
        if (!IsValidIfsc(d.BeneficiaryIfsc)) { missing.Add("Enter a valid IFSC for the payee's bank (e.g. HDFC0001234)."); }
        if (string.IsNullOrWhiteSpace(d.PaymentMode)) { missing.Add("Choose the payment mode."); }
        if (string.IsNullOrWhiteSpace(d.DisburseFromAccount)) { missing.Add("Choose which of your accounts the money is paid from."); }
        if (checklist.Count == 0) { missing.Add("The branch checklist has not been created yet."); }
        else if (checklist.Any(c => c.Flag != "Cleared"))
        {
            missing.Add($"{checklist.Count(c => c.Flag != "Cleared")} branch checklist item(s) still pending.");
        }

        return missing;
    }

    // ---------------------------------------------------------------------
    // Payee bank check
    // ---------------------------------------------------------------------

    /// <param name="State">NotChecked, Verified (the bank confirmed the account and the name), Mismatch (account real, name differs) or Failed.</param>
    /// <param name="Source">"Payee check" or "Stage 3" when the payee is the borrower's own account verified there.</param>
    public sealed record PayeeCheck(string State, string? Note, string? Source)
    {
        public static readonly PayeeCheck NotChecked = new("NotChecked", null, null);
        public bool IsVerified => State == "Verified";
    }

    /// <summary>
    /// The bank check that applies to the payee as currently entered: the latest PAYEE_ACCOUNT answer for exactly this name, account
    /// and IFSC, or the borrower's own Stage 3 penny drop when the payee is that same account. Sandbox answers never count for a
    /// company on production keys.
    /// </summary>
    public static async Task<PayeeCheck> PayeeCheckAsync(LosDbContext db, int companyId, string applicationId, Disbursement d, bool production)
    {
        if (string.IsNullOrWhiteSpace(d.BeneficiaryAccount) || string.IsNullOrWhiteSpace(d.BeneficiaryIfsc))
        {
            return PayeeCheck.NotChecked;
        }

        var fingerprint = PayeeFingerprint(d.BeneficiaryName, d.BeneficiaryAccount, d.BeneficiaryIfsc);
        var evidence = await db.VerificationEvidence.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.ApplicationId == applicationId && e.Subject == PayeeSubject
                        && e.InputFingerprint == fingerprint && (!production || e.Environment == "Production"))
            .OrderByDescending(e => e.Id).FirstOrDefaultAsync();
        if (evidence is not null)
        {
            var state = evidence.Verdict switch { "Verified" => "Verified", "Mismatch" => "Mismatch", _ => "Failed" };
            return new PayeeCheck(state, evidence.Summary, "Payee check");
        }

        // The payee is the borrower's own account, already penny-dropped with a name match at Stage 3.
        var bank = await db.BankDetails.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(b => b.ApplicationId == applicationId);
        if (bank is not null && bank.PennyDropStatus == "Matched"
            && Digits(bank.AccountNumber) == Digits(d.BeneficiaryAccount) && Norm(bank.Ifsc) == Norm(d.BeneficiaryIfsc))
        {
            return new PayeeCheck("Verified", "Borrower's own account, verified by penny drop at Stage 3.", "Stage 3");
        }

        return PayeeCheck.NotChecked;
    }

    /// <summary>Paid payee checks allowed on one file before an Admin has to look at why the payee keeps changing.</summary>
    public const int MaxPayeeChecks = 3;

    /// <summary>
    /// Asks the bank (Digio penny drop) whether the payee's account exists and is in the payee's name. The same name + account + IFSC
    /// is never sent twice: the answer on file is reused. A different payee may be checked, up to <see cref="MaxPayeeChecks"/> per file.
    /// Only while the payment is still being prepared. Never invents an answer: no keys, paused or failed means "not checked".
    /// </summary>
    /// <returns>The check now on file, and a message for the officer when nothing was checked.</returns>
    public static async Task<(PayeeCheck Check, string? Message)> VerifyPayeeAsync(LosDbContext db, DigioClient digio, int companyId, string applicationId,
        string? userId, string? userName)
    {
        var d = await db.Disbursements.AsNoTracking().FirstOrDefaultAsync(x => x.ApplicationId == applicationId);
        if (d is null) { return (PayeeCheck.NotChecked, "Save the payee details first."); }
        if (d.PayoutStatus != Draft) { return (PayeeCheck.NotChecked, "The payment has already been submitted; withdraw it to change the payee."); }
        if (string.IsNullOrWhiteSpace(d.BeneficiaryName) || !IsValidAccount(d.BeneficiaryAccount) || !IsValidIfsc(d.BeneficiaryIfsc))
        {
            return (PayeeCheck.NotChecked, "Enter the payee's name, account number and a valid IFSC first.");
        }

        var production = digio.Credentials(companyId)?.Production ?? false;
        var existing = await PayeeCheckAsync(db, companyId, applicationId, d, production);
        if (existing.State != PayeeCheck.NotChecked.State)
        {
            return (existing, null); // already answered for exactly this payee: nothing is sent, nothing is charged
        }

        var used = await db.VerificationEvidence.IgnoreQueryFilters().AsNoTracking()
            .CountAsync(e => e.CompanyId == companyId && e.ApplicationId == applicationId && e.Subject == PayeeSubject);
        if (used >= MaxPayeeChecks)
        {
            return (PayeeCheck.NotChecked, $"The payee has already been checked {used} times on this file. An Admin can approve the payment "
                                            + "against a cancelled cheque or bank letter instead.");
        }

        var result = await digio.PostAsync(companyId, DigioRates.PennyDrop, "/v4/client/verify/bank_account",
            new
            {
                beneficiary_account_no = Digits(d.BeneficiaryAccount),
                beneficiary_ifsc = d.BeneficiaryIfsc!.Trim().ToUpperInvariant(),
                beneficiary_name = d.BeneficiaryName!.Trim(),
                validation_mode = "PENNY_DROP",
            },
            applicationId);

        if (result.Status == DigioStatus.NotConfigured)
        {
            return (PayeeCheck.NotChecked, "Bank account checks are not set up for this company. The Admin will need to see a cancelled cheque or bank letter.");
        }

        if (result.Status != DigioStatus.Ok)
        {
            return (PayeeCheck.NotChecked, result.Error ?? "The bank check did not complete. Try again later.");
        }

        var verified = result.Bool("verified") == true;
        var verdict = !verified ? "Failed" : result.Bool("fuzzy_match_result") == true ? "Verified" : "Mismatch";
        var bankName = DigioKyc.MaskName(result.Str("beneficiary_name_with_bank"));
        var summary = verdict switch
        {
            "Verified" => "Payee account verified; name matches" + (bankName is null ? "" : $" (name at bank: {bankName})"),
            "Mismatch" => "Account exists but the name does not match" + (bankName is null ? "" : $" (name at bank: {bankName})"),
            _ => "Bank could not verify this account: " + (result.Str("error_msg") ?? "no reason given"),
        };

        db.VerificationEvidence.Add(new VerificationEvidence
        {
            CompanyId = companyId, ApplicationId = applicationId, Subject = PayeeSubject, Verdict = verdict, Summary = summary,
            InputFingerprint = PayeeFingerprint(d.BeneficiaryName, d.BeneficiaryAccount, d.BeneficiaryIfsc),
            Environment = result.Environment ?? "Sandbox", CreatedAt = DateTime.UtcNow,
        });
        Log(db, applicationId, "PayeeChecked", userId, userName, $"A/c ****{Last4(d.BeneficiaryAccount)}: {summary}");
        await db.SaveChangesAsync();

        return (await PayeeCheckAsync(db, companyId, applicationId, d, production), null);
    }

    // ---------------------------------------------------------------------
    // Transitions
    // ---------------------------------------------------------------------

    /// <summary>The officer hands the prepared payment to an Admin. Freezes nothing yet; the Admin's approval does.</summary>
    /// <returns>Null on success, otherwise the reason it was refused.</returns>
    public static async Task<string?> SubmitAsync(LosDbContext db, AdminRequestNotifier notifier, string applicationId, string userId, string? userName,
        DateTime? nowUtc = null)
    {
        var d = await db.Disbursements.FirstOrDefaultAsync(x => x.ApplicationId == applicationId);
        if (d is null) { return "The disbursement details have not been saved yet."; }
        if (d.PayoutStatus != Draft) { return "This payment has already been submitted."; }

        var amount = await NetAmountAsync(db, applicationId, d.TradeAdvance);
        var checklist = await db.PostSanctionChecklists.AsNoTracking().Where(c => c.ApplicationId == applicationId).ToListAsync();
        if (MissingForSubmit(d, amount, checklist) is { Count: > 0 } missing)
        {
            return string.Join(" ", missing);
        }

        var now = nowUtc ?? DateTime.UtcNow;
        d.PayoutStatus = PendingApproval;
        d.SubmittedByUserId = userId;
        d.SubmittedAt = now;
        d.UpdatedAt = now;
        Log(db, applicationId, "Submitted", userId, userName,
            $"{Inr(amount)} to {d.BeneficiaryName} · A/c ****{Last4(d.BeneficiaryAccount)} · {d.BeneficiaryIfsc} · {d.PaymentMode}", now);
        await db.SaveChangesAsync();

        await GateCheckService.RaiseRequestAsync(db, notifier, applicationId, AdminRequest.DisbursementApproval, userId,
            $"Pay {Inr(amount)} to {d.BeneficiaryName} (A/c ****{Last4(d.BeneficiaryAccount)}, {d.BeneficiaryIfsc}) by {d.PaymentMode}.");
        return null;
    }

    /// <summary>
    /// An Admin approves the payment: the amount and the payee details are frozen as they are on file right now. Called from the
    /// request review, which has already refused the person who submitted it. When the payee's account was not verified through the
    /// bank, the Admin must say what they checked instead (a cancelled cheque, a bank letter).
    /// </summary>
    public static async Task<string?> ApproveAsync(LosDbContext db, string applicationId, string adminUserId, string? adminName, string? note,
        bool production, DateTime? nowUtc = null)
    {
        if (!db.IsElevatedUser) { return "Only an Admin can approve a payment."; }

        var d = await db.Disbursements.FirstOrDefaultAsync(x => x.ApplicationId == applicationId);
        if (d is null || d.PayoutStatus != PendingApproval) { return "There is no payment waiting for approval."; }
        if (string.Equals(d.SubmittedByUserId, adminUserId, StringComparison.Ordinal))
        {
            return "You submitted this payment, so another Admin must approve it.";
        }

        var amount = await NetAmountAsync(db, applicationId, d.TradeAdvance);
        var checklist = await db.PostSanctionChecklists.AsNoTracking().Where(c => c.ApplicationId == applicationId).ToListAsync();
        if (MissingForSubmit(d, amount, checklist) is { Count: > 0 } missing)
        {
            return "The payment details are no longer complete: " + string.Join(" ", missing);
        }

        var companyId = await db.Applications.IgnoreQueryFilters().Where(a => a.Id == applicationId).Select(a => a.CompanyId).FirstAsync();
        var payee = await PayeeCheckAsync(db, companyId, applicationId, d, production);
        if (!payee.IsVerified && string.IsNullOrWhiteSpace(note))
        {
            return "The payee's account was not verified through the bank. Write what you checked (for example a cancelled cheque or the "
                   + "bank's letter) in the note, then approve.";
        }

        var now = nowUtc ?? DateTime.UtcNow;
        d.PayoutStatus = Approved;
        d.ApprovedByUserId = adminUserId;
        d.ApprovedAt = now;
        d.ApprovedAmount = amount;
        d.ApprovedFingerprint = PayoutFingerprint(d, amount);
        d.ApprovalNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        d.UpdatedAt = now;
        Log(db, applicationId, "Approved", adminUserId, adminName,
            $"{Inr(amount)} · payee check: {(payee.IsVerified ? "verified" : "not verified")}" + (d.ApprovalNote is { } n ? $" · Note: {n}" : ""), now);
        await db.SaveChangesAsync();
        return null;
    }

    /// <summary>An Admin turns the payment down; it goes back to Draft so the officer can correct it.</summary>
    public static async Task RejectAsync(LosDbContext db, string applicationId, string adminUserId, string? adminName, string? note, DateTime? nowUtc = null)
    {
        var d = await db.Disbursements.FirstOrDefaultAsync(x => x.ApplicationId == applicationId);
        if (d is null || d.PayoutStatus != PendingApproval) { return; }

        var now = nowUtc ?? DateTime.UtcNow;
        ResetToDraft(d, now);
        Log(db, applicationId, "Rejected", adminUserId, adminName, note, now);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Takes a submitted or approved (not yet released) payment back to Draft: the officer who submitted it, or any Admin. An approved
    /// payment can only be cancelled by an Admin, and only before it is released. Closes any pending approval request.
    /// </summary>
    public static async Task<string?> WithdrawAsync(LosDbContext db, string applicationId, string userId, string? userName, bool isAdmin, string? reason,
        DateTime? nowUtc = null)
    {
        var d = await db.Disbursements.FirstOrDefaultAsync(x => x.ApplicationId == applicationId);
        if (d is null || d.PayoutStatus is not (PendingApproval or Approved)) { return "There is nothing to withdraw."; }
        if (d.PayoutStatus == Approved && !isAdmin) { return "Only an Admin can cancel an approved payment."; }
        if (d.PayoutStatus == PendingApproval && !isAdmin && !string.Equals(d.SubmittedByUserId, userId, StringComparison.Ordinal))
        {
            return "Only the person who submitted it, or an Admin, can withdraw it.";
        }

        if (string.IsNullOrWhiteSpace(reason)) { return "Say why it is being withdrawn."; }

        var now = nowUtc ?? DateTime.UtcNow;
        var wasApproved = d.PayoutStatus == Approved;
        ResetToDraft(d, now);
        foreach (var r in await db.AdminRequests.Where(r => r.ApplicationId == applicationId && r.RequestType == AdminRequest.DisbursementApproval
                                                            && r.Status == AdminRequest.Pending).ToListAsync())
        {
            r.Status = AdminRequest.Withdrawn;
            r.ReviewedByUserId = userId;
            r.ReviewedAt = now;
            r.ReviewNote = reason.Trim();
        }

        Log(db, applicationId, wasApproved ? "ApprovalCancelled" : "Withdrawn", userId, userName, reason.Trim(), now);
        await db.SaveChangesAsync();
        return null;
    }

    /// <summary>
    /// An Admin confirms the money has been paid from the bank: the payment reference (UTR / cheque number) and the date it was paid
    /// are recorded, the application becomes Disbursed, and the loan account and its EMI schedule open — all in one transaction.
    /// Refused if the amount or payee changed after approval, the checklist is no longer clear, the reference is malformed or was
    /// already used on another loan, or the date is impossible.
    /// </summary>
    /// <param name="digioOverride">Called when the Digio gate (agreement signed / mandate registered) is not met: an Admin may still
    /// release, and this records that override. Null means "refuse" instead.</param>
    public static async Task<string?> ReleaseAsync(LosDbContext db, string applicationId, string adminUserId, string? adminName, string? reference,
        DateOnly? paidOn, DateOnly todayIst, bool production, DateTime? nowUtc = null)
    {
        if (!db.IsElevatedUser) { return "Only an Admin can confirm that the money was paid."; }

        var d = await db.Disbursements.FirstOrDefaultAsync(x => x.ApplicationId == applicationId);
        var application = await db.Applications.FirstOrDefaultAsync(a => a.Id == applicationId);
        if (d is null || application is null) { return "The disbursement details have not been saved yet."; }
        if (application.Disbursed || d.PayoutStatus == Released) { return "This loan has already been released."; }
        if (d.PayoutStatus != Approved) { return "An Admin must approve the payment before it is released."; }

        var amount = await NetAmountAsync(db, applicationId, d.TradeAdvance);
        if (d.ApprovedAmount != amount || d.ApprovedFingerprint != PayoutFingerprint(d, amount))
        {
            return $"The amount or payee details changed after approval (approved {Inr(d.ApprovedAmount ?? 0m)}, now {Inr(amount)}). "
                   + "Cancel the approval and submit it again.";
        }

        if (await db.PostSanctionChecklists.AnyAsync(c => c.ApplicationId == applicationId && c.Flag != "Cleared"))
        {
            return "A branch checklist item is no longer cleared.";
        }

        var refText = (reference ?? string.Empty).Trim().ToUpperInvariant();
        if (!IsValidReference(refText, d.PaymentMode))
        {
            return string.Equals(d.PaymentMode, "Cheque", StringComparison.OrdinalIgnoreCase)
                ? "Enter the 6-digit cheque number."
                : "Enter the bank's payment reference (UTR): 12 to 22 letters and digits, from your bank statement or payment advice.";
        }

        if (await db.Disbursements.IgnoreQueryFilters().AnyAsync(x => x.ApplicationId != applicationId && x.Utr == refText
                                                                      && x.Application!.CompanyId == application.CompanyId))
        {
            return "That payment reference is already recorded on another loan. Check you are not recording the same payment twice.";
        }

        var approvedOn = DateOnly.FromDateTime((d.ApprovedAt ?? DateTime.UtcNow).AddMinutes(330));
        if (paidOn is not { } paid || paid > todayIst || paid < approvedOn)
        {
            return $"Enter the date the money was paid: not before the approval ({approvedOn:d MMM yyyy}) and not in the future.";
        }

        if (d.FirstEmiDate is { } firstEmi && paid >= firstEmi)
        {
            return $"The first EMI date ({firstEmi:d MMM yyyy}) is not after the payment date. The repayment schedule and agreement must be "
                   + "corrected before release.";
        }

        var decision = await db.ApprovalDecisions.AsNoTracking().FirstOrDefaultAsync(x => x.ApplicationId == applicationId);
        if (decision?.SanctionedAmount is not { } principal || decision.SanctionedRoi is not { } roi || decision.SanctionedTenure is not { } tenure)
        {
            return "The sanction terms are missing.";
        }

        var gate = await DigioReleaseGate.CheckAsync(db, applicationId, production);

        var now = nowUtc ?? DateTime.UtcNow;
        await using var tx = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync() : null;

        // Claim the release atomically: of two Admins confirming at the same moment, exactly one gets past this line.
        var claimed = await db.Disbursements.IgnoreQueryFilters().Where(x => x.ApplicationId == applicationId && x.PayoutStatus == Approved)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.PayoutStatus, Released));
        if (claimed != 1)
        {
            return "This loan has already been released.";
        }

        d.PayoutStatus = Released;
        d.Utr = refText;
        d.ValueDate = paid;
        d.ReleasedByUserId = adminUserId;
        d.ReleasedAt = now;
        d.UpdatedAt = now;
        application.Disbursed = true;
        application.UpdatedAt = now;

        if (!gate.CanAdvance)
        {
            await DigioReleaseGate.LogOverrideAsync(db, applicationId, adminUserId, gate.BlockedReason ?? "");
            Log(db, applicationId, "DigioGateOverridden", adminUserId, adminName, gate.BlockedReason, now);
        }

        var account = await LoanAccountService.OpenAsync(db, application, principal, roi, tenure, d.FirstEmiDate, d.EmiDayOfMonth,
            d.FirstEmiOverride, d.EmiRoundedTo, now);
        Log(db, applicationId, "Released", adminUserId, adminName,
            $"{Inr(amount)} paid on {paid:d MMM yyyy} · ref {refText}" + (account is null ? " · loan account NOT opened (check first EMI date)" : ""), now);

        await db.SaveChangesAsync();
        if (tx is not null) { await tx.CommitAsync(); }
        return null;
    }

    // ---------------------------------------------------------------------
    // What a page save may change
    // ---------------------------------------------------------------------

    /// <summary>
    /// Copies the officer's edits onto the stored row, but only what the payout state allows. In Draft everything is editable.
    /// Once submitted or approved, the amount-, payee- and EMI-related fields are frozen (withdraw to change them). Once released,
    /// nothing payment-related changes again: the UTR, value date and payee are the record of what was paid.
    /// Documents (memo, welcome letter, agreement file) stay editable throughout; they are not money.
    /// </summary>
    public static void ApplyPageEdits(Disbursement stored, Disbursement page)
    {
        if (stored.PayoutStatus == Draft)
        {
            stored.Type = page.Type;
            stored.BeneficiaryName = page.BeneficiaryName;
            stored.BeneficiaryAccount = page.BeneficiaryAccount;
            stored.BeneficiaryIfsc = string.IsNullOrWhiteSpace(page.BeneficiaryIfsc) ? null : page.BeneficiaryIfsc.Trim().ToUpperInvariant();
            stored.BeneficiaryBank = page.BeneficiaryBank;
            stored.PaymentMode = page.PaymentMode;
            stored.ValueDate = page.ValueDate; // the planned payment date; ReleaseAsync records the real one
            stored.DisburseFromAccount = page.DisburseFromAccount;
            stored.TradeAdvance = page.TradeAdvance;
            stored.FirstEmiDate = page.FirstEmiDate;
            stored.EmiDayOfMonth = page.EmiDayOfMonth;
            stored.FirstEmiOverride = page.FirstEmiOverride;
            stored.EmiRoundedTo = page.EmiRoundedTo;
        }

        // The UTR is written only by ReleaseAsync, together with the date the money was actually paid.
    }

    /// <summary>Copies the frozen payout fields from the stored row onto the page's copy, so a frozen form shows what is on file.</summary>
    public static void CopyPayoutFields(Disbursement from, Disbursement to)
    {
        to.Type = from.Type;
        to.BeneficiaryName = from.BeneficiaryName;
        to.BeneficiaryAccount = from.BeneficiaryAccount;
        to.BeneficiaryIfsc = from.BeneficiaryIfsc;
        to.BeneficiaryBank = from.BeneficiaryBank;
        to.PaymentMode = from.PaymentMode;
        to.ValueDate = from.ValueDate;
        to.Utr = from.Utr;
        to.DisburseFromAccount = from.DisburseFromAccount;
        to.TradeAdvance = from.TradeAdvance;
        to.FirstEmiDate = from.FirstEmiDate;
        to.EmiDayOfMonth = from.EmiDayOfMonth;
        to.FirstEmiOverride = from.FirstEmiOverride;
        to.EmiRoundedTo = from.EmiRoundedTo;
    }

    /// <summary>
    /// Applies a checklist change from the page. Refused (returns false, row untouched) after the payment was submitted, and for an
    /// Admin-only item when the user is not an Admin. Clearing stamps who and when; reopening clears both. Remarks follow the same rule.
    /// </summary>
    public static bool ApplyChecklistEdit(PostSanctionChecklist stored, PostSanctionChecklist page, string payoutStatus, bool isAdmin, string? userId,
        DateOnly todayIst)
    {
        if (payoutStatus != Draft) { return false; }

        var flagChanged = stored.Flag != page.Flag;
        if (!flagChanged && stored.Remarks == page.Remarks) { return true; }
        if (AdminOnlyChecklistItems.Contains(stored.Item) && !isAdmin) { return false; }

        if (flagChanged)
        {
            stored.Flag = page.Flag;
            if (page.Flag == "Cleared")
            {
                stored.ClearedOn = todayIst;
                stored.ClearedByUserId = userId;
            }
            else
            {
                stored.ClearedOn = null;
                stored.ClearedByUserId = null;
            }
        }

        stored.Remarks = page.Remarks;
        return true;
    }

    public static DateOnly TodayIst(DateTime? nowUtc = null) => DateOnly.FromDateTime((nowUtc ?? DateTime.UtcNow).AddMinutes(330));

    private static void ResetToDraft(Disbursement d, DateTime now)
    {
        d.PayoutStatus = Draft;
        d.SubmittedByUserId = null;
        d.SubmittedAt = null;
        d.ApprovedByUserId = null;
        d.ApprovedAt = null;
        d.ApprovedAmount = null;
        d.ApprovedFingerprint = null;
        d.ApprovalNote = null;
        d.UpdatedAt = now;
    }

    // ---------------------------------------------------------------------
    // Audit trail
    // ---------------------------------------------------------------------

    public static void Log(LosDbContext db, string applicationId, string action, string? userId, string? userName, string? detail, DateTime? nowUtc = null) =>
        db.DisbursementEvents.Add(new DisbursementEvent
        {
            ApplicationId = applicationId, At = nowUtc ?? DateTime.UtcNow, Action = action, UserId = userId, UserName = userName,
            Detail = detail is null ? null : detail.Length > 500 ? detail[..500] : detail,
        });

    public static string ActionLabel(string action) => action switch
    {
        "Submitted" => "Submitted for approval",
        "Approved" => "Approved by Admin",
        "Rejected" => "Rejected by Admin",
        "Withdrawn" => "Withdrawn",
        "ApprovalCancelled" => "Approval cancelled",
        "Released" => "Funds released",
        "PayeeChecked" => "Payee account checked",
        "DigioGateOverridden" => "Released without signed agreement / mandate (Admin override)",
        "ChecklistCleared" => "Checklist item cleared",
        "ChecklistReopened" => "Checklist item reopened",
        _ => action,
    };

    public static string Inr(decimal amount) => "₹" + amount.ToString("N2", new CultureInfo("en-IN"));

    public static string Last4(string? account)
    {
        var digits = Digits(account);
        return digits.Length <= 4 ? digits : digits[^4..];
    }
}

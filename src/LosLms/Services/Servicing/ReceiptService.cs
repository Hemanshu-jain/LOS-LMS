using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LosLms.Services;

/// <summary>
/// The ONLY way an instalment becomes Paid or PartPaid (docs/digio/03 section 1: "the only thing that marks an instalment paid").
/// A NACH success (from the confirmed Digio state) and an Admin-approved manual cash/cheque receipt both end in
/// <see cref="ApplyToInstalment"/>. Nothing else in the code base assigns those statuses (a test scans for it).
/// </summary>
public static class ReceiptService
{
    /// <summary>
    /// Adds the receipt for a successful debit and marks the instalment. One receipt per presentation: a replayed success finds the
    /// existing one (and the unique index on PresentationId backs that up). The caller saves.
    /// </summary>
    public static async Task<Receipt?> RecordNachAsync(LosDbContext db, Presentation presentation, string? utr, decimal amount, DateOnly paidOn, DateTime? nowUtc = null)
    {
        if (await db.Receipts.IgnoreQueryFilters().AnyAsync(r => r.PresentationId == presentation.Id))
        {
            return null;
        }

        var instalment = await db.Instalments.IgnoreQueryFilters().Include(i => i.LoanAccount).FirstOrDefaultAsync(i => i.Id == presentation.InstalmentId);
        if (instalment is null)
        {
            return null;
        }

        var receipt = new Receipt
        {
            CompanyId = presentation.CompanyId, LoanAccountId = instalment.LoanAccountId, InstalmentId = instalment.Id,
            ReceiptNo = await NextNumberAsync(db, presentation.CompanyId, paidOn), Source = "NACH", Amount = amount, ReceivedOn = paidOn,
            Reference = utr, PresentationId = presentation.Id, Status = "Approved", CreatedAt = nowUtc ?? DateTime.UtcNow,
        };
        db.Receipts.Add(receipt);
        ApplyToInstalment(instalment, amount, paidOn);
        return receipt;
    }

    /// <summary>A manual cash / cheque receipt. It counts for nothing until an Admin approves it.</summary>
    public static async Task<Receipt> RecordManualAsync(
        LosDbContext db, LoanAccount account, int? instalmentId, string source, decimal amount, DateOnly receivedOn, string? reference, string? userId, DateTime? nowUtc = null)
    {
        if (source is not ("Cash" or "Cheque" or "Manual") || amount <= 0)
        {
            throw new ArgumentException("A manual receipt needs a source (Cash, Cheque or Manual) and a positive amount.");
        }

        var receipt = new Receipt
        {
            CompanyId = account.CompanyId, LoanAccountId = account.Id, InstalmentId = instalmentId, Source = source, Amount = amount,
            ReceivedOn = receivedOn, Reference = reference?.Trim(), Status = "Pending", RecordedByUserId = userId,
            ReceiptNo = await NextNumberAsync(db, account.CompanyId, receivedOn), CreatedAt = nowUtc ?? DateTime.UtcNow,
        };
        db.Receipts.Add(receipt);
        return receipt;
    }

    /// <summary>
    /// Admin approval of a manual receipt. Refused unless the caller is an Admin, and never by whoever recorded it (the same
    /// segregation of duties as the Admin Inbox). Returns null when approved, else the reason.
    /// </summary>
    public static async Task<string?> ApproveManualAsync(LosDbContext db, int receiptId, string adminUserId, bool isElevated)
    {
        var receipt = await db.Receipts.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == receiptId);
        if (receipt is null || receipt.Status != "Pending")
        {
            return "That receipt is not waiting for approval.";
        }

        if (!isElevated)
        {
            return "Only an Admin can approve a manual receipt.";
        }

        if (string.Equals(receipt.RecordedByUserId, adminUserId, StringComparison.Ordinal))
        {
            return "Whoever recorded a receipt cannot approve it.";
        }

        receipt.Status = "Approved";
        receipt.ApprovedByUserId = adminUserId;
        if (receipt.InstalmentId is { } id && await db.Instalments.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == id) is { } instalment)
        {
            ApplyToInstalment(instalment, receipt.Amount, receipt.ReceivedOn);
        }

        await db.SaveChangesAsync();
        return null;
    }

    /// <summary>The single writer of Paid / PartPaid.</summary>
    internal static void ApplyToInstalment(Instalment instalment, decimal amount, DateOnly paidOn)
    {
        instalment.PaidAmount += amount;
        instalment.PaidOn = paidOn;
        instalment.Status = instalment.PaidAmount >= instalment.Emi ? "Paid" : "PartPaid";
    }

    /// <summary>{CompanyCode}-RCPT-{yyyy}-{00001}. The unique index on (CompanyId, ReceiptNo) turns a race into a retry, never a duplicate.</summary>
    public static async Task<string> NextNumberAsync(LosDbContext db, int companyId, DateOnly on)
    {
        var code = await db.Companies.IgnoreQueryFilters().Where(c => c.Id == companyId).Select(c => c.Code).FirstOrDefaultAsync();
        var prefix = $"{(string.IsNullOrWhiteSpace(code) ? "LMS" : code)}-RCPT-{on.Year}-";
        var last = await db.Receipts.IgnoreQueryFilters().Where(r => r.CompanyId == companyId && r.ReceiptNo.StartsWith(prefix))
            .OrderByDescending(r => r.ReceiptNo).Select(r => r.ReceiptNo).FirstOrDefaultAsync();
        var next = last is not null && int.TryParse(last[prefix.Length..], out var n) ? n + 1 : 1;
        return prefix + next.ToString("00000");
    }

    /// <summary>Renders the receipt PDF (once) and returns its file key under the application's uploads.</summary>
    public static async Task<string?> EnsurePdfAsync(LosDbContext db, IFileStore files, int receiptId)
    {
        var receipt = await db.Receipts.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == receiptId);
        if (receipt is null || receipt.Status != "Approved")
        {
            return null;
        }

        if (receipt.PdfPath is not null)
        {
            return receipt.PdfPath;
        }

        var account = await db.LoanAccounts.IgnoreQueryFilters().FirstAsync(a => a.Id == receipt.LoanAccountId);
        var company = await db.Companies.IgnoreQueryFilters().Where(c => c.Id == receipt.CompanyId).Select(c => c.Name).FirstOrDefaultAsync();
        var customer = await db.Parties.IgnoreQueryFilters().Where(p => p.ApplicationId == account.ApplicationId && p.PartyType == "Applicant").Select(p => p.FullName).FirstOrDefaultAsync();
        var no = receipt.InstalmentId is { } id ? await db.Instalments.IgnoreQueryFilters().Where(i => i.Id == id).Select(i => (int?)i.No).FirstOrDefaultAsync() : null;

        var bytes = Pdf(company, customer, account.LoanAccountNo, receipt, no);
        receipt.PdfPath = FileKey.Normalize(await files.SaveUploadAsync(account.ApplicationId, "receipts", receipt.ReceiptNo + ".pdf", bytes));
        await db.SaveChangesAsync();
        return receipt.PdfPath;
    }

    public static byte[] Pdf(string? company, string? customer, string loanAccountNo, Receipt r, int? instalmentNo) =>
        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A5.Landscape());
            page.Margin(32);
            page.DefaultTextStyle(t => t.FontSize(11));
            page.Header().Column(h =>
            {
                h.Item().Text(string.IsNullOrWhiteSpace(company) ? "Payment receipt" : company.Trim()).FontSize(15).Bold();
                h.Item().Text("PAYMENT RECEIPT").FontSize(9).FontColor("#5b6472");
                h.Item().PaddingTop(6).LineHorizontal(1);
            });
            page.Content().PaddingTop(12).Column(c =>
            {
                c.Spacing(6);
                c.Item().Text($"Receipt no.: {r.ReceiptNo}");
                c.Item().Text($"Date: {r.ReceivedOn:dd MMM yyyy}");
                c.Item().Text($"Received from: {customer ?? "the borrower"}");
                c.Item().Text($"Loan account: {loanAccountNo}" + (instalmentNo is { } n ? $"   ·   EMI no. {n}" : ""));
                c.Item().Text($"Amount: Rs {r.Amount:N2}").Bold();
                c.Item().Text($"Mode: {r.Source}" + (string.IsNullOrWhiteSpace(r.Reference) ? "" : $"   ·   Ref: {r.Reference}"));
            });
            page.Footer().AlignCenter().Text("System-generated receipt.").FontSize(8).FontColor("#93999a");
        })).GeneratePdf();
}

using System.Security.Claims;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LosLms.Tests;

/// <summary>
/// The payout control: an officer prepares, a different Admin approves the exact amount and payee, an Admin confirms the bank
/// reference, and nothing about the money can change underneath any of those steps.
/// </summary>
public sealed class DisbursementWorkflowTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"payout-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly AdminRequestNotifier _notifier = new();
    private static readonly DateTime Now = new(2026, 11, 2, 6, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 11, 2);

    public DisbursementWorkflowTests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using var db = new LosDbContext(_options, TenantContext.ForSeeding());
        db.Database.EnsureCreated();
        foreach (var id in new[] { "officer", "admin", "admin2" })
        {
            db.Users.Add(new ApplicationUser { Id = id, UserName = id, NormalizedUserName = id.ToUpperInvariant(), DisplayName = id, CompanyId = 1 });
        }

        db.Applications.Add(new Application { Id = "A1", CompanyId = 1 });
        db.Applications.Add(new Application { Id = "A2", CompanyId = 1, Disbursed = true });
        db.ApprovalDecisions.Add(new ApprovalDecision { ApplicationId = "A1", SanctionedAmount = 500_000m, SanctionedRoi = 14m, SanctionedTenure = 36 });
        db.Charges.Add(new Charge { ApplicationId = "A1", Head = "Processing fee", Amount = 5_000m, Gst = 900m });
        db.Charges.Add(new Charge { ApplicationId = "A1", Head = "Waived fee", Amount = 1_000m, Gst = 180m, Waived = true });
        db.Disbursements.Add(new Disbursement
        {
            ApplicationId = "A1", Type = "Direct to dealer", BeneficiaryName = "Sri Sai Motors", BeneficiaryAccount = "50100012345678",
            BeneficiaryIfsc = "HDFC0001234", PaymentMode = "NEFT", DisburseFromAccount = "ICICI current — 001", TradeAdvance = 10_000m,
            FirstEmiDate = new DateOnly(2026, 12, 5), EmiDayOfMonth = 5,
        });
        db.Disbursements.Add(new Disbursement { ApplicationId = "A2", Utr = "HDFCN52026110100001", PayoutStatus = DisbursementWorkflow.Released });
        foreach (var item in new[] { "CAM approval", "Rate & PF approval", "Agreement executed" })
        {
            db.PostSanctionChecklists.Add(new PostSanctionChecklist { ApplicationId = "A1", Item = item, Owner = "x", Flag = "Cleared" });
        }

        db.SaveChanges();
    }

    private const decimal Expected = 500_000m - 5_900m - 10_000m;

    [Fact]
    public void NetAmount_DeductsUnwaivedChargesWithGst_AndTheTradeAdvance()
    {
        var charges = new[] { new Charge { Amount = 5_000m, Gst = 900m }, new Charge { Amount = 1_000m, Gst = 180m, Waived = true } };
        Assert.Equal(Expected, DisbursementWorkflow.NetAmount(500_000m, charges, 10_000m));
    }

    [Theory]
    [InlineData("HDFC0001234", true)]
    [InlineData("hdfc0001234", true)]
    [InlineData("HDFC1001234", false)] // fifth character must be zero
    [InlineData("HDF0001234", false)]
    [InlineData(null, false)]
    public void Ifsc_IsValidated(string? ifsc, bool valid) => Assert.Equal(valid, DisbursementWorkflow.IsValidIfsc(ifsc));

    [Theory]
    [InlineData("HDFCN52026110212345", "NEFT", true)]
    [InlineData("123456", "Cheque", true)]
    [InlineData("12345", "Cheque", false)]
    [InlineData("ABC123", "NEFT", false)] // too short to be a bank reference
    [InlineData("HDFC-N5202611", "NEFT", false)]
    public void PaymentReference_IsValidated(string reference, string mode, bool valid) =>
        Assert.Equal(valid, DisbursementWorkflow.IsValidReference(reference, mode));

    [Fact]
    public async Task FullPath_OfficerSubmits_AnotherAdminApproves_AdminReleases_AndTheLoanAccountOpens()
    {
        using (var officer = As("officer", admin: false))
        {
            Assert.Null(await DisbursementWorkflow.SubmitAsync(officer, _notifier, "A1", "officer", "Officer", Now));
        }

        using (var admin = As("admin", admin: true))
        {
            var request = await admin.AdminRequests.SingleAsync(r => r.RequestType == AdminRequest.DisbursementApproval);
            Assert.Equal(AdminRequest.Pending, request.Status);

            Assert.Null(await DisbursementWorkflow.ApproveAsync(admin, "A1", "admin", "Admin", "Cancelled cheque seen", false, Now));
            var d = await admin.Disbursements.AsNoTracking().SingleAsync(x => x.ApplicationId == "A1");
            Assert.Equal(DisbursementWorkflow.Approved, d.PayoutStatus);
            Assert.Equal(Expected, d.ApprovedAmount);

            Assert.Null(await DisbursementWorkflow.ReleaseAsync(admin, "A1", "admin", "Admin", "hdfcn52026110212345", Today, Today, false, Now));
        }

        using var read = As("admin", admin: true);
        var released = await read.Disbursements.AsNoTracking().SingleAsync(x => x.ApplicationId == "A1");
        Assert.Equal(DisbursementWorkflow.Released, released.PayoutStatus);
        Assert.Equal("HDFCN52026110212345", released.Utr);
        Assert.Equal(Today, released.ValueDate);
        Assert.Equal("admin", released.ReleasedByUserId);
        Assert.True((await read.Applications.SingleAsync(a => a.Id == "A1")).Disbursed);
        Assert.Single(read.LoanAccounts.Where(a => a.ApplicationId == "A1"));
        Assert.Equal(new[] { "Submitted", "Approved", "Released" },
            await read.DisbursementEvents.Where(e => e.ApplicationId == "A1").OrderBy(e => e.Id).Select(e => e.Action).ToListAsync());
    }

    [Fact]
    public async Task AnOfficerCannotApproveOrRelease_AndTheSubmitterCannotApproveTheirOwn()
    {
        using (var officer = As("officer", admin: false))
        {
            Assert.Null(await DisbursementWorkflow.SubmitAsync(officer, _notifier, "A1", "officer", "Officer", Now));
            Assert.NotNull(await DisbursementWorkflow.ApproveAsync(officer, "A1", "officer", "Officer", "ok", false, Now));
        }

        using var admin = As("admin", admin: true);
        await Reset(admin);
        Assert.Null(await DisbursementWorkflow.SubmitAsync(admin, _notifier, "A1", "admin", "Admin", Now));
        Assert.Contains("another Admin", await DisbursementWorkflow.ApproveAsync(admin, "A1", "admin", "Admin", "ok", false, Now));

        using var officer2 = As("officer", admin: false);
        Assert.NotNull(await DisbursementWorkflow.ReleaseAsync(officer2, "A1", "officer", "Officer", "HDFCN52026110212345", Today, Today, false, Now));
    }

    [Fact]
    public async Task AnUnverifiedPayee_NeedsTheAdminsNote()
    {
        await Submit();
        using var admin = As("admin", admin: true);
        Assert.Contains("not verified", await DisbursementWorkflow.ApproveAsync(admin, "A1", "admin", "Admin", null, false, Now));
        Assert.Null(await DisbursementWorkflow.ApproveAsync(admin, "A1", "admin", "Admin", "Bank letter on file", false, Now));
    }

    [Fact]
    public async Task AVerifiedPayee_NeedsNoNote_ButSandboxEvidenceDoesNotCountForProduction()
    {
        await Submit();
        using var admin = As("admin", admin: true);
        var d = await admin.Disbursements.AsNoTracking().SingleAsync(x => x.ApplicationId == "A1");
        admin.VerificationEvidence.Add(new VerificationEvidence
        {
            CompanyId = 1, ApplicationId = "A1", Subject = DisbursementWorkflow.PayeeSubject, Verdict = "Verified", Environment = "Sandbox",
            InputFingerprint = DisbursementWorkflow.PayeeFingerprint(d.BeneficiaryName, d.BeneficiaryAccount, d.BeneficiaryIfsc), CreatedAt = Now,
        });
        await admin.SaveChangesAsync();

        Assert.NotNull(await DisbursementWorkflow.ApproveAsync(admin, "A1", "admin", "Admin", null, production: true, Now));
        Assert.Null(await DisbursementWorkflow.ApproveAsync(admin, "A1", "admin", "Admin", null, production: false, Now));
    }

    [Fact]
    public async Task ABorrowersOwnStage3VerifiedAccount_CountsAsAVerifiedPayee()
    {
        using var db = As("admin", admin: true);
        db.BankDetails.Add(new BankDetail { ApplicationId = "A1", AccountNumber = "50100012345678", Ifsc = "HDFC0001234", PennyDropStatus = "Matched" });
        await db.SaveChangesAsync();
        var d = await db.Disbursements.AsNoTracking().SingleAsync(x => x.ApplicationId == "A1");

        var check = await DisbursementWorkflow.PayeeCheckAsync(db, 1, "A1", d, production: true);
        Assert.True(check.IsVerified);
        Assert.Equal("Stage 3", check.Source);

        d.BeneficiaryAccount = "50100099999999"; // a different account is not covered by the borrower's check
        Assert.False((await DisbursementWorkflow.PayeeCheckAsync(db, 1, "A1", d, production: true)).IsVerified);
    }

    [Fact]
    public async Task ChangingTheAmountOrPayeeAfterApproval_BlocksRelease()
    {
        await SubmitAndApprove();

        // A Stage 7 charge edited after approval changes what would be paid.
        using (var db = As("admin", admin: true))
        {
            db.Charges.Add(new Charge { ApplicationId = "A1", Head = "Late addition", Amount = 2_000m });
            await db.SaveChangesAsync();
            Assert.Contains("changed after approval",
                await DisbursementWorkflow.ReleaseAsync(db, "A1", "admin", "Admin", "HDFCN52026110212345", Today, Today, false, Now));
        }

        // The payee row changed directly (e.g. by a stale save path) is caught by the fingerprint too.
        using (var db = As("admin", admin: true))
        {
            db.Charges.RemoveRange(db.Charges.Where(c => c.Head == "Late addition"));
            var d = await db.Disbursements.SingleAsync(x => x.ApplicationId == "A1");
            d.BeneficiaryAccount = "50100099999999";
            await db.SaveChangesAsync();
            Assert.Contains("changed after approval",
                await DisbursementWorkflow.ReleaseAsync(db, "A1", "admin", "Admin", "HDFCN52026110212345", Today, Today, false, Now));
        }
    }

    [Fact]
    public async Task ThePageSave_CannotChangeThePayeeOnceSubmitted()
    {
        await Submit();
        using var db = As("officer", admin: false);
        var stored = await db.Disbursements.SingleAsync(x => x.ApplicationId == "A1");
        var page = new Disbursement { BeneficiaryName = "Someone Else", BeneficiaryAccount = "99999999999", BeneficiaryIfsc = "SBIN0000001", TradeAdvance = 0m };

        DisbursementWorkflow.ApplyPageEdits(stored, page);

        Assert.Equal("Sri Sai Motors", stored.BeneficiaryName);
        Assert.Equal("50100012345678", stored.BeneficiaryAccount);
        Assert.Equal(10_000m, stored.TradeAdvance);
    }

    [Fact]
    public void ThePageSave_NeverWritesTheUtr()
    {
        var stored = new Disbursement { PayoutStatus = DisbursementWorkflow.Draft };
        DisbursementWorkflow.ApplyPageEdits(stored, new Disbursement { Utr = "HDFCN52026110212345", BeneficiaryName = "X" });
        Assert.Null(stored.Utr);
        Assert.Equal("X", stored.BeneficiaryName);
    }

    [Fact]
    public async Task ADuplicateUtr_OrAnImpossibleDate_IsRefused()
    {
        await SubmitAndApprove();
        using var db = As("admin", admin: true);

        Assert.Contains("already recorded on another loan",
            await DisbursementWorkflow.ReleaseAsync(db, "A1", "admin", "Admin", "HDFCN52026110100001", Today, Today, false, Now));
        Assert.Contains("not in the future",
            await DisbursementWorkflow.ReleaseAsync(db, "A1", "admin", "Admin", "HDFCN52026110212345", Today.AddDays(1), Today, false, Now));
        Assert.Contains("not before the approval",
            await DisbursementWorkflow.ReleaseAsync(db, "A1", "admin", "Admin", "HDFCN52026110212345", Today.AddDays(-3), Today, false, Now));
        Assert.Contains("UTR", await DisbursementWorkflow.ReleaseAsync(db, "A1", "admin", "Admin", "12", Today, Today, false, Now));
        Assert.False((await db.Applications.SingleAsync(a => a.Id == "A1")).Disbursed);
    }

    [Fact]
    public async Task ASecondRelease_IsRefused_AndOpensNoSecondAccount()
    {
        await SubmitAndApprove();
        using (var db = As("admin", admin: true))
        {
            Assert.Null(await DisbursementWorkflow.ReleaseAsync(db, "A1", "admin", "Admin", "HDFCN52026110212345", Today, Today, false, Now));
        }

        using (var db = As("admin2", admin: true))
        {
            Assert.Contains("already been released",
                await DisbursementWorkflow.ReleaseAsync(db, "A1", "admin2", "Admin 2", "HDFCN52026110299999", Today, Today, false, Now));
            Assert.Single(db.LoanAccounts.Where(a => a.ApplicationId == "A1"));
            Assert.Equal("HDFCN52026110212345", (await db.Disbursements.SingleAsync(x => x.ApplicationId == "A1")).Utr);
        }
    }

    [Fact]
    public async Task Withdraw_ReturnsToDraft_ClosesTheRequest_AndOnlyAnAdminCancelsAnApproval()
    {
        await SubmitAndApprove();
        using (var officer = As("officer", admin: false))
        {
            Assert.Contains("Only an Admin", await DisbursementWorkflow.WithdrawAsync(officer, "A1", "officer", "Officer", false, "wrong account", Now));
        }

        using var admin = As("admin", admin: true);
        Assert.Contains("why", await DisbursementWorkflow.WithdrawAsync(admin, "A1", "admin", "Admin", true, " ", Now));
        Assert.Null(await DisbursementWorkflow.WithdrawAsync(admin, "A1", "admin", "Admin", true, "Dealer changed bank", Now));
        var d = await admin.Disbursements.AsNoTracking().SingleAsync(x => x.ApplicationId == "A1");
        Assert.Equal(DisbursementWorkflow.Draft, d.PayoutStatus);
        Assert.Null(d.ApprovedFingerprint);
    }

    [Fact]
    public async Task Reject_ReturnsToDraft()
    {
        await Submit();
        using var admin = As("admin", admin: true);
        await DisbursementWorkflow.RejectAsync(admin, "A1", "admin", "Admin", "Wrong dealer", Now);
        Assert.Equal(DisbursementWorkflow.Draft, (await admin.Disbursements.AsNoTracking().SingleAsync(x => x.ApplicationId == "A1")).PayoutStatus);
    }

    [Fact]
    public async Task Submit_IsRefused_WhileAChecklistItemIsPending_OrTheIfscIsMissing()
    {
        using (var db = As("admin", admin: true))
        {
            var row = await db.PostSanctionChecklists.FirstAsync(c => c.Item == "Agreement executed");
            row.Flag = "Pending";
            (await db.Disbursements.SingleAsync(x => x.ApplicationId == "A1")).BeneficiaryIfsc = null;
            await db.SaveChangesAsync();
        }

        using var officer = As("officer", admin: false);
        var refused = await DisbursementWorkflow.SubmitAsync(officer, _notifier, "A1", "officer", "Officer", Now);
        Assert.Contains("IFSC", refused);
        Assert.Contains("checklist", refused);
    }

    [Fact]
    public void Checklist_AdminOnlyItems_NeedAnAdmin_AndNothingChangesAfterSubmit()
    {
        var cam = new PostSanctionChecklist { Item = "CAM approval", Flag = "Pending" };
        var page = new PostSanctionChecklist { Item = "CAM approval", Flag = "Cleared" };

        Assert.False(DisbursementWorkflow.ApplyChecklistEdit(cam, page, DisbursementWorkflow.Draft, isAdmin: false, "officer", Today));
        Assert.Equal("Pending", cam.Flag);

        Assert.True(DisbursementWorkflow.ApplyChecklistEdit(cam, page, DisbursementWorkflow.Draft, isAdmin: true, "admin", Today));
        Assert.Equal("Cleared", cam.Flag);
        Assert.Equal("admin", cam.ClearedByUserId);
        Assert.Equal(Today, cam.ClearedOn);

        var agreement = new PostSanctionChecklist { Item = "Agreement executed", Flag = "Cleared" };
        Assert.False(DisbursementWorkflow.ApplyChecklistEdit(agreement, new PostSanctionChecklist { Flag = "Pending" },
            DisbursementWorkflow.PendingApproval, isAdmin: true, "admin", Today));
        Assert.Equal("Cleared", agreement.Flag);
    }

    // ---- plumbing ----

    private async Task Submit()
    {
        using var officer = As("officer", admin: false);
        Assert.Null(await DisbursementWorkflow.SubmitAsync(officer, _notifier, "A1", "officer", "Officer", Now));
    }

    private async Task SubmitAndApprove()
    {
        await Submit();
        using var admin = As("admin", admin: true);
        Assert.Null(await DisbursementWorkflow.ApproveAsync(admin, "A1", "admin", "Admin", "Cancelled cheque seen", false, Now));
    }

    private static async Task Reset(LosDbContext db)
    {
        var d = await db.Disbursements.SingleAsync(x => x.ApplicationId == "A1");
        d.PayoutStatus = DisbursementWorkflow.Draft;
        db.AdminRequests.RemoveRange(db.AdminRequests);
        await db.SaveChangesAsync();
    }

    private LosDbContext As(string userId, bool admin)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, admin ? TenantContext.AdminRole : TenantContext.StaffRole),
            new Claim(TenantContext.CompanyIdClaim, "1"),
        }, "TestAuth"));
        var tenant = new TenantContext(new Auth(user), new HttpContextAccessor(), new ActingCompanyStore());
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        return new LosDbContext(_options, tenant);
    }

    private sealed class Auth(ClaimsPrincipal user) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(user));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }
}

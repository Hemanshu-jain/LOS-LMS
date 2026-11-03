using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LosLms.Tests;

/// <summary>The Admin approval between Stage 2 and Verification: nothing moves on, and nothing is sent to Digio, until an Admin approves.</summary>
public sealed class StageApprovalTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"stageappr-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly AdminRequestNotifier _notifier = new();

    public StageApprovalTests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using var db = Seeding();
        db.Database.EnsureCreated();
        foreach (var id in new[] { "officer", "admin" })
        {
            db.Users.Add(new ApplicationUser { Id = id, UserName = id, NormalizedUserName = id.ToUpperInvariant(), DisplayName = id, CompanyId = 1 });
        }

        db.Applications.Add(new Application { Id = "A1", CompanyId = 1, CurrentStage = 2 });
        db.Applications.Add(new Application { Id = "A2", CompanyId = 1, CurrentStage = 5 });
        db.SaveChanges();
    }

    private LosDbContext Seeding() => new(_options, TenantContext.ForSeeding());

    // ForSeeding() is a SuperAdmin; a plain officer context is needed to prove the non-Admin path.
    private LosDbContext Officer()
    {
        var user = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "officer"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, TenantContext.StaffRole),
            new System.Security.Claims.Claim(TenantContext.CompanyIdClaim, "1"),
        }, "TestAuth"));
        var tenant = new TenantContext(new Auth(user), new Microsoft.AspNetCore.Http.HttpContextAccessor(), new ActingCompanyStore());
        tenant.EnsureLoadedAsync().GetAwaiter().GetResult();
        return new LosDbContext(_options, tenant);
    }

    [Fact]
    public async Task AnOfficerCompletingStage2_RaisesTheRequest_AndTheFileStaysAtStage2()
    {
        using var db = Officer();
        Assert.Equal(StageApprovalService.Outcome.Pending, await StageApprovalService.SubmitAsync(db, _notifier, "A1", "officer", "Stages done"));

        Assert.Equal(2, (await db.Applications.AsNoTracking().SingleAsync(a => a.Id == "A1")).CurrentStage);
        Assert.False(await StageApprovalService.IsApprovedAsync(db, "A1"));
        Assert.Single(db.AdminRequests.Where(r => r.RequestType == AdminRequest.ProceedApproval && r.Status == AdminRequest.Pending));
        Assert.Equal((true, null), await StageApprovalService.StateAsync(db, "A1"));
    }

    [Fact]
    public async Task CompletingAgain_WhileWaiting_RaisesNoSecondRequest()
    {
        using var db = Officer();
        await StageApprovalService.SubmitAsync(db, _notifier, "A1", "officer", "x");
        Assert.Equal(StageApprovalService.Outcome.AlreadyWaiting, await StageApprovalService.SubmitAsync(db, _notifier, "A1", "officer", "x"));
        Assert.Single(db.AdminRequests.Where(r => r.RequestType == AdminRequest.ProceedApproval));
    }

    [Fact]
    public async Task ApprovalMovesTheFileToVerification()
    {
        using (var officer = Officer()) { await StageApprovalService.SubmitAsync(officer, _notifier, "A1", "officer", "x"); }

        using var admin = Seeding();
        await StageApprovalService.ApplyApprovalAsync(admin, "A1");

        Assert.Equal(3, (await admin.Applications.AsNoTracking().SingleAsync(a => a.Id == "A1")).CurrentStage);
        Assert.True(await StageApprovalService.IsApprovedAsync(admin, "A1"));
    }

    [Fact]
    public async Task AnAdminCompletingStage2_NeedsNoOneElse_AndIsRecorded()
    {
        using var admin = Seeding();
        Assert.Equal(StageApprovalService.Outcome.Approved, await StageApprovalService.SubmitAsync(admin, _notifier, "A1", "admin", "x"));

        Assert.Equal(3, (await admin.Applications.AsNoTracking().SingleAsync(a => a.Id == "A1")).CurrentStage);
        Assert.Single(admin.AdminRequests.Where(r => r.RequestType == AdminRequest.ProceedApproval && r.Status == AdminRequest.Approved));
    }

    [Fact]
    public async Task AFileAlreadyPastStage2_IsTreatedAsApproved_AndNeverMovedBack()
    {
        using var db = Officer();
        Assert.Equal(StageApprovalService.Outcome.Approved, await StageApprovalService.SubmitAsync(db, _notifier, "A2", "officer", "x"));
        Assert.Equal(5, (await db.Applications.AsNoTracking().SingleAsync(a => a.Id == "A2")).CurrentStage);
        await StageApprovalService.ApplyApprovalAsync(db, "A2");
        Assert.Equal(5, (await db.Applications.AsNoTracking().SingleAsync(a => a.Id == "A2")).CurrentStage);
    }

    [Fact]
    public async Task ResendAsync_WhenAlreadyPending_NudgesExistingRequest()
    {
        using var db = Officer();
        await StageApprovalService.SubmitAsync(db, _notifier, "A1", "officer", "Initial request");

        var outcome = await StageApprovalService.ResendAsync(db, _notifier, "A1", "officer");
        Assert.Equal(StageApprovalService.Outcome.Pending, outcome);

        var req = await db.AdminRequests.SingleAsync(r => r.ApplicationId == "A1");
        Assert.Equal(AdminRequest.Pending, req.Status);
        Assert.Contains("Re-sent", req.RequestReason);
    }

    [Fact]
    public async Task ResendAsync_AfterDenial_RaisesNewRequestFlaggedAsRetriggered()
    {
        using (var db = Officer())
        {
            await StageApprovalService.SubmitAsync(db, _notifier, "A1", "officer", "First request");
        }

        // Admin denies it
        using (var admin = Seeding())
        {
            var req = await admin.AdminRequests.SingleAsync(r => r.ApplicationId == "A1");
            req.Status = AdminRequest.Denied;
            req.ReviewNote = "Please fix address proof";
            await admin.SaveChangesAsync();
        }

        using (var db = Officer())
        {
            // Officer checks state
            var (waiting, deniedWhy) = await StageApprovalService.StateAsync(db, "A1");
            Assert.False(waiting);
            Assert.Equal("Please fix address proof", deniedWhy);

            // Officer re-sends request
            var outcome = await StageApprovalService.ResendAsync(db, _notifier, "A1", "officer", "Fixed address proof");
            Assert.Equal(StageApprovalService.Outcome.Pending, outcome);

            // State is now waiting again
            var (waitingAfter, deniedAfter) = await StageApprovalService.StateAsync(db, "A1");
            Assert.True(waitingAfter);
            Assert.Null(deniedAfter);

            // There are now two requests: one Denied and one Pending with [Re-triggered]
            var allReqs = await db.AdminRequests.AsNoTracking().Where(r => r.ApplicationId == "A1").OrderBy(r => r.Id).ToListAsync();
            Assert.Equal(2, allReqs.Count);
            Assert.Equal(AdminRequest.Denied, allReqs[0].Status);
            Assert.Equal(AdminRequest.Pending, allReqs[1].Status);
            Assert.Contains("[Re-triggered]", allReqs[1].RequestReason);
        }
    }

    private sealed class Auth(System.Security.Claims.ClaimsPrincipal user) : Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider
    {
        public override Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(user));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }
}

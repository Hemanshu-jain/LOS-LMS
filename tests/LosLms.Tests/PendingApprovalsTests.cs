using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LosLms.Tests;

public sealed class PendingApprovalsTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"pa-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<LosDbContext> _options;

    public PendingApprovalsTests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using var db = Open();
        db.Database.EnsureCreated();
        db.Applications.AddRange(new Application { Id = "A1", CompanyId = 1 }, new Application { Id = "A2", CompanyId = 1 });
        db.SaveChanges();
    }

    private LosDbContext Open() => new(_options, TenantContext.ForSeeding());

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { File.Delete(_file); } catch (IOException) { }
    }

    private static AdminRequest Request(string app, string type, string status, string? reason = null) =>
        new() { ApplicationId = app, RequestType = type, Status = status, RequestedAt = DateTime.UtcNow, RequestReason = reason };

    [Theory]
    [InlineData(AdminRequest.CibilBypass, 1)]
    [InlineData(AdminRequest.ProceedApproval, 2)]
    [InlineData(AdminRequest.MakeModelCapBypass, 2)]
    [InlineData(AdminRequest.KycBypass, 3)]
    [InlineData(AdminRequest.RcuReview, 5)]
    [InlineData(AdminRequest.SanctionApproval, 7)]
    [InlineData(AdminRequest.DisbursementApproval, 8)]
    public void StageOf_NamesTheStageWhereTheApprovalIsAsked(string type, int expected) =>
        Assert.Equal(expected, PendingApprovals.StageOf(type, 4));

    [Fact]
    public void StageOf_UnknownKindUsesTheFilesCurrentStage_AndSeveralRequestsShowTheEarliest()
    {
        Assert.Equal(4, PendingApprovals.StageOf(AdminRequest.Reject, 4));
        Assert.Equal(2, PendingApprovals.StageOf(new[] { AdminRequest.SanctionApproval, AdminRequest.ProceedApproval }, 7));
        Assert.Equal(6, PendingApprovals.StageOf(Array.Empty<string>(), 6));
    }

    [Fact]
    public void ResendNotes_CountsTheNudges()
    {
        Assert.Equal(0, PendingApprovals.ResendNotes(null));
        Assert.Equal(2, PendingApprovals.ResendNotes("please approve | Re-sent 31 Oct 2026 06:54 UTC by u1. | Re-sent 01 Nov 2026 09:00 UTC by u1."));
    }

    [Fact]
    public async Task RetriggerCounts_CountDenialsAndNudges_AndLeaveFirstTimeRequestsOut()
    {
        using var db = Open();
        db.AdminRequests.AddRange(
            Request("A1", AdminRequest.ProceedApproval, AdminRequest.Denied),
            Request("A1", AdminRequest.ProceedApproval, AdminRequest.Denied),
            Request("A1", AdminRequest.ProceedApproval, AdminRequest.Pending, "[Re-triggered] again | Re-sent 01 Nov 2026 09:00 UTC by u1."),
            Request("A2", AdminRequest.ProceedApproval, AdminRequest.Pending, "first time"),
            Request("A2", AdminRequest.SanctionApproval, AdminRequest.Pending, "other kind"));
        db.SaveChanges();
        var pending = db.AdminRequests.Where(r => r.Status == AdminRequest.Pending).OrderBy(r => r.Id).ToList();

        var counts = await PendingApprovals.RetriggerCountsAsync(db, pending);

        Assert.Equal(3, counts[pending[0].Id]);          // two earlier denials + one nudge
        Assert.DoesNotContain(pending[1].Id, counts.Keys);
        Assert.DoesNotContain(pending[2].Id, counts.Keys);
    }

    [Fact]
    public void RetriggerLabel_ShowsTheCountOnlyWhenRepeated()
    {
        Assert.Equal("Re-triggered", PendingApprovals.RetriggerLabel(1));
        Assert.Equal("Re-triggered ×3", PendingApprovals.RetriggerLabel(3));
    }
}

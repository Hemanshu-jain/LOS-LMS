using System.Text;
using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LosLms.Tests;

/// <summary>The TG Transport record: a link plus the officer's tick, date, name and optional screenshot. Nothing is automated.</summary>
public sealed class TgPortalTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"tg-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly MemoryFiles _files = new();

    public TgPortalTests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using var db = Ctx();
        db.Database.EnsureCreated();
        db.Applications.Add(new Application { Id = "A1", CompanyId = 1 });
        db.SaveChanges();
    }

    private LosDbContext Ctx() => new(_options, TenantContext.ForSeeding());

    [Fact]
    public void TheLink_IsTheTgVehicleSearch_OverHttps()
    {
        Assert.Equal("https://tgtransport.net/TGCFSTONLINE/Reports/VehicleRegistrationSearch.aspx", TgPortalService.Url);
    }

    [Fact]
    public async Task Tick_CreatesTheRowIfMissing_RecordsWhoAndWhen_AndUntickClearsIt()
    {
        var when = new DateTime(2026, 10, 29, 6, 30, 0, DateTimeKind.Utc);
        using (var db = Ctx()) { await TgPortalService.MarkAsync(db, "A1", true, "u1", "Officer One", when); }

        using (var read = Ctx())
        {
            var s = read.SecurityDetails.Single();
            Assert.Equal((true, when, "u1", "Officer One"), (s.TgPortalVerified, s.TgPortalVerifiedAt, s.TgPortalVerifiedByUserId, s.TgPortalVerifiedBy));
        }

        using (var db = Ctx()) { await TgPortalService.MarkAsync(db, "A1", false, "u2", "Officer Two"); }
        using var after = Ctx();
        var cleared = after.SecurityDetails.Single();
        Assert.Equal((false, null, null, null), (cleared.TgPortalVerified, cleared.TgPortalVerifiedAt, cleared.TgPortalVerifiedByUserId, cleared.TgPortalVerifiedBy));
    }

    [Fact]
    public async Task Tick_LeavesTheRestOfTheSecurityRowAlone()
    {
        using (var db = Ctx()) { db.SecurityDetails.Add(new SecurityDetail { ApplicationId = "A1", RegNo = "TS09AB1234", ChassisNo = "CH1" }); db.SaveChanges(); }

        using (var db = Ctx()) { await TgPortalService.MarkAsync(db, "A1", true, "u1", "Officer"); }

        using var read = Ctx();
        Assert.Equal(1, read.SecurityDetails.Count());
        Assert.Equal(("TS09AB1234", "CH1"), (read.SecurityDetails.Single().RegNo, read.SecurityDetails.Single().ChassisNo));
    }

    [Fact]
    public async Task Screenshot_IsKeptInTheFileStore_ForARealImageOrPdf_AndRefusedOtherwise()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3 };
        string? key;
        using (var db = Ctx()) { (key, _) = await TgPortalService.AttachAsync(db, _files, "A1", "shot.PNG", png); }

        Assert.StartsWith("App_Data/uploads/A1/rcportal/tg-", key);
        Assert.EndsWith(".png", key);
        Assert.Equal(png, _files.Saved[key!]);
        using (var read = Ctx()) { Assert.Equal(key, read.SecurityDetails.Single().TgPortalScreenshotPath); }

        using var db2 = Ctx();
        Assert.NotNull((await TgPortalService.AttachAsync(db2, _files, "A1", "evil.exe", png)).Error);                                  // wrong type
        Assert.NotNull((await TgPortalService.AttachAsync(db2, _files, "A1", "fake.png", Encoding.ASCII.GetBytes("not an image"))).Error); // wrong bytes
        Assert.NotNull((await TgPortalService.AttachAsync(db2, _files, "A1", "empty.pdf", [])).Error);
        Assert.Single(_files.Saved);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }

    private sealed class MemoryFiles : IFileStore
    {
        public Dictionary<string, byte[]> Saved { get; } = [];

        public Task SaveAsync(string key, byte[] bytes)
        {
            Saved[FileKey.Normalize(key)] = bytes;
            return Task.CompletedTask;
        }

        public Task<Stream?> OpenReadAsync(string key) =>
            Task.FromResult<Stream?>(Saved.TryGetValue(FileKey.Normalize(key), out var b) ? new MemoryStream(b) : null);
    }
}

using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LosLms.Tests;

public sealed class UiBatch1031Tests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"ui1031-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<LosDbContext> _options;

    public UiBatch1031Tests()
    {
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite($"Data Source={_file};Pooling=False").Options;
        using var db = Open();
        db.Database.EnsureCreated();
        db.Applications.Add(new Application { Id = "A1", CompanyId = 1 });
        db.SaveChanges();
    }

    private LosDbContext Open() => new(_options, TenantContext.ForSeeding());

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { File.Delete(_file); } catch (IOException) { }
    }

    [Fact]
    public async Task Resend_BumpsAndNotesThePendingRequest_AndNothingWhenNonePending()
    {
        using var db = Open();
        var notifier = new AdminRequestNotifier();
        var notified = 0;
        notifier.Changed += _ => { notified++; return Task.CompletedTask; };

        Assert.False(await GateCheckService.ResendRequestAsync(db, notifier, "A1", AdminRequest.SanctionApproval, "u1"));

        await GateCheckService.RaiseRequestAsync(db, notifier, "A1", AdminRequest.SanctionApproval, null, "please approve");
        var first = db.AdminRequests.Single().RequestedAt;
        notified = 0;

        var later = first.AddHours(3);
        Assert.True(await GateCheckService.ResendRequestAsync(db, notifier, "A1", AdminRequest.SanctionApproval, "u1", later));

        var row = db.AdminRequests.Single();
        Assert.Equal(later, row.RequestedAt);
        Assert.Contains("please approve", row.RequestReason);
        Assert.Contains("Re-sent", row.RequestReason);
        Assert.Equal(1, notified);
    }

    [Fact]
    public void Disbursement_DefaultsFillBlanksOnly()
    {
        var today = new DateOnly(2026, 10, 31);
        var bank = new BankDetail { AccountNumber = "50100123456789" };
        var d = new Disbursement { Type = DisbursementDefaults.DirectToCustomer };

        DisbursementDefaults.Apply(d, " Raj Kumar ", bank, today, ["HDFC — Ops"]);

        Assert.Equal(today, d.ValueDate);
        Assert.Equal("NEFT", d.PaymentMode);
        Assert.Equal("Raj Kumar", d.BeneficiaryName);
        Assert.Equal("50100123456789", d.BeneficiaryAccount);
        Assert.Equal("HDFC — Ops", d.DisburseFromAccount);

        var kept = new Disbursement
        {
            Type = DisbursementDefaults.DirectToCustomer, BeneficiaryName = "Dealer Ltd", PaymentMode = "RTGS",
            ValueDate = new DateOnly(2026, 11, 2), DisburseFromAccount = "ICICI",
        };
        DisbursementDefaults.Apply(kept, "Raj Kumar", bank, today, ["HDFC — Ops", "ICICI"]);
        Assert.Equal("Dealer Ltd", kept.BeneficiaryName);
        Assert.Equal("RTGS", kept.PaymentMode);
        Assert.Equal(new DateOnly(2026, 11, 2), kept.ValueDate);
        Assert.Equal("ICICI", kept.DisburseFromAccount);
    }

    [Fact]
    public void Disbursement_DealerPaymentDoesNotGetTheCustomersAccount()
    {
        var d = new Disbursement { Type = "Direct to dealer" };

        DisbursementDefaults.Apply(d, "Raj Kumar", new BankDetail { AccountNumber = "1" }, new DateOnly(2026, 10, 31), ["A", "B"]);

        Assert.Equal("", d.BeneficiaryName);
        Assert.Equal("", d.BeneficiaryAccount);
        Assert.Null(d.DisburseFromAccount);
    }

    [Theory]
    [InlineData(2026, 10, 31, 5, 2026, 11, 5)]
    [InlineData(2026, 10, 3, 5, 2026, 10, 5)]
    [InlineData(2026, 10, 5, 5, 2026, 11, 5)]
    [InlineData(2026, 1, 31, 31, 2026, 2, 28)]
    public void NextDayOfMonth_IsTheNextOccurrence(int y, int m, int d, int day, int ey, int em, int ed) =>
        Assert.Equal(new DateOnly(ey, em, ed), DisbursementDefaults.NextDayOfMonth(new DateOnly(y, m, d), day));

    [Fact]
    public void DisbursementMemo_RendersAPdf()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var data = new DisbursementMemoData(
            "Acme Finance", "LN-1", "Raj Kumar", 1_400_000m, 13.5m, 48,
            [("Processing fee", 21_000m, false), ("Doc charges", 500m, true)], 0m, 1_379_000m,
            "Direct to customer", "Raj Kumar", "50100123456789", "HDFC — Ops", "NEFT", new DateOnly(2026, 10, 31), null,
            "R. Kulkarni", "Admin", new DateOnly(2026, 10, 30), 38_000m, new DateOnly(2026, 11, 5), new DateOnly(2030, 10, 5), 48, 1_824_000m);

        var pdf = DisbursementMemoPdf.Build(data);

        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }
}

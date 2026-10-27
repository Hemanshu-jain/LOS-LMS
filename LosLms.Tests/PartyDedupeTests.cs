using LosLms.Data;
using LosLms.Models;
using LosLms.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LosLms.Tests;

/// <summary>The re-application rule (open application blocks, closed one means "returning"), company-scoped.</summary>
public sealed class PartyDedupeTests : IDisposable
{
    private const int A = LosDbContext.SeedCompanyId;
    private const int B = 2;

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<LosDbContext> _options;

    public PartyDedupeTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<LosDbContext>().UseSqlite(_connection).Options;
        using var db = Ctx();
        db.Database.EnsureCreated();
        db.Companies.Add(new Company { Id = B, Name = "Company B" });
        db.SaveChanges();
    }

    // ---- truth table ------------------------------------------------------------------------------

    [Fact]
    public async Task NoOtherParty_Passes()
    {
        var me = Add("LN-1", "New", pan: "ABCDE1234F");
        var r = await Check(me);
        Assert.Equal(("Pass", 0, false), (r.Status, r.Earlier.Count, r.MobileWarning));
    }

    [Theory]
    [InlineData("New")]
    [InlineData("In progress")]
    public async Task OpenApplicationInTheSameCompany_Blocks_OnPan_OrAadhaar(string otherStatus)
    {
        var me = Add("LN-1", "New", pan: "ABCDE1234F", aadhaar: "111122223333");
        Add("LN-0", otherStatus, pan: "ABCDE1234F");
        Assert.Equal("Fail", (await Check(me)).Status);

        var viaAadhaar = Add("LN-2", "New", aadhaar: "999988887777");
        Add("LN-3", otherStatus, aadhaar: "999988887777");
        Assert.Equal("Fail", (await Check(viaAadhaar)).Status);
    }

    [Fact]
    public async Task ClosedEarlierApplications_AreAllowed_AndCounted_SanctionedOrRejected()
    {
        var me = Add("LN-1", "New", pan: "ABCDE1234F");
        Add("LN-0", "Sanctioned", pan: "ABCDE1234F");
        Add("LN-9", "Rejected", pan: "ABCDE1234F");

        var r = await Check(me);

        Assert.Equal("Pass", r.Status);
        Assert.Equal(new[] { "Rejected", "Sanctioned" }.OrderBy(x => x), r.Earlier.Select(e => e.Status).OrderBy(x => x));
        Assert.False(r.MobileWarning);
    }

    [Fact]
    public async Task AnOpenApplicationWins_EvenWhenClosedOnesExist()
    {
        var me = Add("LN-1", "New", pan: "ABCDE1234F");
        Add("LN-0", "Sanctioned", pan: "ABCDE1234F");
        Add("LN-5", "In progress", pan: "ABCDE1234F");
        Assert.Equal("Fail", (await Check(me)).Status);
    }

    [Fact]
    public async Task AnotherCompany_IsInvisible_NoFail_NoCount_NoWarning()
    {
        var me = Add("LN-1", "New", pan: "ABCDE1234F", mobile: "9000000001");
        Add("LN-X", "In progress", company: B, pan: "ABCDE1234F", mobile: "9000000001");
        Add("LN-Y", "Sanctioned", company: B, pan: "ABCDE1234F");

        var r = await Check(me);

        Assert.Equal(("Pass", 0, false), (r.Status, r.Earlier.Count, r.MobileWarning));
    }

    [Fact]
    public async Task MobileOnlyMatch_WarnsButNeverBlocks()
    {
        var me = Add("LN-1", "New", pan: "ABCDE1234F", mobile: "9000000001");
        Add("LN-0", "In progress", pan: "ZZZZZ9999Z", mobile: "9000000001");

        var r = await Check(me);

        Assert.Equal(("Pass", true), (r.Status, r.MobileWarning));
        Assert.Empty(r.Earlier);
    }

    [Fact]
    public async Task SameApplication_SharedMobileIsIgnored_ButSharedPanStillFails()
    {
        var applicant = Add("LN-1", "New", pan: "ABCDE1234F", mobile: "9000000001");
        Add("LN-1", "New", partyType: "CoApplicant", pan: "QWERT5678Y", mobile: "9000000001");
        var spouse = await Check(applicant);
        Assert.Equal(("Pass", false), (spouse.Status, spouse.MobileWarning));

        Add("LN-1", "New", partyType: "Guarantor", pan: "ABCDE1234F"); // the same person entered twice
        Assert.Equal("Fail", (await Check(applicant)).Status);
    }

    [Fact]
    public async Task AStatusThatIsNotSavedYet_CanBeSuppliedToTheCheck()
    {
        var me = Add("LN-1", "New", pan: "ABCDE1234F");
        Add("LN-0", "In progress", pan: "ABCDE1234F");

        await using var db = Ctx();
        var party = await db.Parties.FirstAsync(p => p.ApplicationId == "LN-1");
        var r = await PartyDedupeService.CheckAsync(db, party, ("LN-0", "Sanctioned"));

        Assert.Equal(("Pass", 1), (r.Status, r.Earlier.Count));
    }

    [Fact]
    public async Task SanctioningTheOtherApplication_FlipsTheBlockedDraftToPass()
    {
        var draft = Add("LN-2", "New", pan: "ABCDE1234F");
        Add("LN-1", "In progress", pan: "ABCDE1234F");
        await using (var db = Ctx())
        {
            var p = await db.Parties.FirstAsync(x => x.ApplicationId == "LN-2");
            p.DedupeStatus = "Fail";
            await db.SaveChangesAsync();
        }

        await using (var db = Ctx())
        {
            await PartyDedupeService.RecomputeForStatusChangeAsync(db, "LN-1", "Sanctioned");
            await db.SaveChangesAsync();
        }

        await using var check = Ctx();
        Assert.Equal("Pass", (await check.Parties.FirstAsync(x => x.ApplicationId == "LN-2")).DedupeStatus);
    }

    // ---- copy details -----------------------------------------------------------------------------

    [Fact]
    public void CopyBlanks_FillsOnlyBlanks_NeverIdentifiersFlagsOrFiles()
    {
        var source = new Party
        {
            FullName = "Old Name", Gender = "Male", City = "Pune", Address1 = "12 Old Road", MonthlyIncome = 50000,
            YearsInJob = 4, Email = "old@x.test", Pan = "ABCDE1234F", Aadhaar = "111122223333", Mobile = "9000000001",
            PhotoPath = "App_Data/uploads/LN-0/Applicant/photo.png", AadhaarVerified = true, DedupeStatus = "Fail",
        };
        var target = new PartyForm { FullName = "New Name", City = "  ", MonthlyIncome = null, YearsInJob = 9 };

        PartyDedupeService.CopyBlanks(source, target);

        Assert.Equal("New Name", target.FullName);        // already filled: kept
        Assert.Equal(9, target.YearsInJob);               // already filled: kept
        Assert.Equal("Pune", target.City);                // blank (whitespace): filled
        Assert.Equal("Male", target.Gender);
        Assert.Equal(50000m, target.MonthlyIncome);
        Assert.Equal("old@x.test", target.Email);
        Assert.Null(target.Pan);                           // identifiers are not copied
        Assert.Null(target.Aadhaar);
        Assert.Null(target.Mobile);
        Assert.Null(target.PhotoPath);                     // files are not shared between applications
        Assert.Equal("NotRun", target.DedupeStatus);
        Assert.False(target.AadhaarVerified);
    }

    // ---- Aadhaar display --------------------------------------------------------------------------

    [Theory]
    [InlineData("123456781234", "XXXX XXXX 1234")]
    [InlineData(" 123456781234 ", "XXXX XXXX 1234")]
    [InlineData("12345", "12345")]            // half typed: shown as is so editing works
    [InlineData("1234567812ab", "1234567812ab")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void AadhaarMask_ShowsOnlyTheLastFourOfAFullNumber(string? input, string expected) =>
        Assert.Equal(expected, AadhaarMask.Of(input));

    // ---- helpers ----------------------------------------------------------------------------------

    private LosDbContext Ctx() => new(_options, TenantContext.ForSeeding());

    private Party Add(string applicationId, string status, int company = A, string partyType = "Applicant",
        string? pan = null, string? aadhaar = null, string? mobile = null)
    {
        using var db = Ctx();
        if (!db.Applications.IgnoreQueryFilters().Any(a => a.Id == applicationId))
        {
            db.Applications.Add(new Application { Id = applicationId, CompanyId = company, Status = status, CreatedAt = DateTime.UtcNow });
        }

        var party = new Party { ApplicationId = applicationId, PartyType = partyType, Pan = pan, Aadhaar = aadhaar, Mobile = mobile };
        db.Parties.Add(party);
        db.SaveChanges();
        return party;
    }

    private async Task<DedupeResult> Check(Party party)
    {
        await using var db = Ctx();
        return await PartyDedupeService.CheckAsync(db, await db.Parties.FirstAsync(p => p.Id == party.Id));
    }

    public void Dispose() => _connection.Dispose();
}

using LosLms.Models;
using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

public sealed class OwnerUx1030Tests
{
    [Fact]
    public void VerifiedPanAndAadhaar_MapToTheirPartyAndLatestDate()
    {
        var parties = new[] { new Party { Id = 5, PartyType = "Applicant" }, new Party { Id = 6, PartyType = "CoApplicant" } };
        var evidence = new[]
        {
            new VerificationEvidence { PartyId = 5, Subject = "PAN", Verdict = "Verified", CreatedAt = new DateTime(2026, 10, 1) },
            new VerificationEvidence { PartyId = 5, Subject = "PAN", Verdict = "Verified", CreatedAt = new DateTime(2026, 10, 5) },
            new VerificationEvidence { PartyId = 5, Subject = "AADHAAR", Verdict = "Verified", CreatedAt = new DateTime(2026, 10, 2) },
            new VerificationEvidence { PartyId = 6, Subject = "PAN", Verdict = "Mismatch", CreatedAt = new DateTime(2026, 10, 3) },
            new VerificationEvidence { PartyId = 6, Subject = "AML", Verdict = "Verified", CreatedAt = new DateTime(2026, 10, 3) },
        };

        var map = ChecklistVerification.VerifiedOn(evidence, parties);

        Assert.Equal(new DateTime(2026, 10, 5), map[("Applicant", "Pan")]);
        Assert.Equal(new DateTime(2026, 10, 2), map[("Applicant", "Aadhaar")]);
        Assert.Equal(2, map.Count);
    }

    [Fact]
    public void OnlyChoice_SelectsTheSingleOptionOnlyWhenBlank()
    {
        Assert.Equal("Hyderabad", OnlyChoice.IfBlank(null, ["Hyderabad"]));
        Assert.Equal("Pune", OnlyChoice.IfBlank("Pune", ["Hyderabad"]));
        Assert.Null(OnlyChoice.IfBlank("", ["A", "B"]) is { Length: > 0 } s ? s : null);
        Assert.Equal("", OnlyChoice.IfBlank("", []));
    }

    [Fact]
    public void Business_StartsFromApplicant_NotTheLender()
    {
        var lender = new Company { Name = "Kishore Finance", Constitution = "Private limited company", Gstin = "27AAAAA0000A1Z5" };
        var applicant = new Party { EmployerName = " Test Traders ", CustomerCategory = "Proprietorship" };
        var b = new Business();

        BusinessPrefill.Apply(b, applicant, lender);

        Assert.Equal("Test Traders", b.FirmName);
        Assert.Equal("Proprietorship", b.Constitution);
        Assert.Equal("", b.Gstin);
    }

    [Fact]
    public void Business_ClearsAnEarlierLenderFill_AndKeepsOfficerEntries()
    {
        var lender = new Company { Name = "Kishore Finance", Constitution = "Private limited company", Gstin = "27AAAAA0000A1Z5" };
        var poisoned = new Business { FirmName = "kishore finance", Constitution = "Private limited company", Gstin = "27AAAAA0000A1Z5" };

        BusinessPrefill.Apply(poisoned, new Party { EmployerName = "Test Traders" }, lender);

        Assert.Equal("Test Traders", poisoned.FirmName);
        Assert.Equal("", poisoned.Constitution);
        Assert.Equal("", poisoned.Gstin);

        var edited = new Business { FirmName = "Own Firm", Constitution = "HUF" };
        BusinessPrefill.Apply(edited, new Party { EmployerName = "Test Traders", CustomerCategory = "Proprietorship" }, lender);
        Assert.Equal("Own Firm", edited.FirmName);
        Assert.Equal("HUF", edited.Constitution);
    }
}

public sealed class OwnerUx1030FollowUpTests
{
    [Theory]
    [InlineData("Proprietorship", true)]
    [InlineData("Individual", true)]
    [InlineData("Partnership firm", false)]
    [InlineData("Private limited company", false)]
    [InlineData(null, false)]
    public void SoleProprietorship_NeedsNoGstinOrIncorporationDate(string? constitution, bool expected) =>
        Assert.Equal(expected, BusinessPrefill.IsSoleProprietorship(constitution));

    [Fact]
    public void IndividualApplicant_GetsProprietorshipConstitution()
    {
        var b = new Business();

        BusinessPrefill.Apply(b, new Party { CustomerCategory = "Individual" }, null);

        Assert.Equal("Proprietorship", b.Constitution);
    }
}

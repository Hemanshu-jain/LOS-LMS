using LosLms.Models;
using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

/// <summary>Business constitution rules: a sole proprietorship needs no GSTIN or incorporation date, and an individual applicant defaults to proprietorship.</summary>
public sealed class BusinessConstitutionRulesTests
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

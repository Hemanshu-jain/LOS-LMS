using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

/// <summary>
/// The company code prefixes every application id, so it must be name-derived where possible and always
/// unique. These pin the generator's behaviour: the natural first-three-letters choice, the fallbacks
/// when it is taken, short/blank names, and case-insensitive clash detection.
/// </summary>
public sealed class CompanyCodeTests
{
    [Fact]
    public void UsesFirstThreeLetters_WhenFree()
    {
        Assert.Equal("KIS", CompanyCode.Generate("Kishore Finance", new HashSet<string>()));
    }

    [Fact]
    public void IgnoresNonLetters_WhenDerivingCode()
    {
        // Digits, spaces and punctuation are skipped; the first three actual letters win.
        Assert.Equal("SUN", CompanyCode.Generate("  Sunrise-24 Motors", new HashSet<string>()));
    }

    [Fact]
    public void ShortName_IsPadded()
    {
        Assert.Equal("AXX", CompanyCode.Generate("A", new HashSet<string>()));
    }

    [Fact]
    public void Collision_ProducesADifferentCodeKeepingTheFirstLetters()
    {
        var code = CompanyCode.Generate("Kishore Finance", new HashSet<string> { "KIS" });

        Assert.NotEqual("KIS", code);
        Assert.Equal(3, code.Length);
        Assert.StartsWith("KI", code); // first two letters are preserved
    }

    [Fact]
    public void ClashDetection_IsCaseInsensitive()
    {
        var code = CompanyCode.Generate("Kishore", new HashSet<string> { "kis" });

        // The lower-case "kis" must be treated as taken, so the upper-case "KIS" is not reused.
        Assert.NotEqual("KIS", code);
    }

    [Fact]
    public void ManyCompaniesWithTheSameName_AllGetDistinctCodes()
    {
        var taken = new HashSet<string>();
        for (var i = 0; i < 50; i++)
        {
            var code = CompanyCode.Generate("Kishore Finance", taken);
            Assert.Equal(3, code.Length);
            Assert.True(taken.Add(code), $"Code {code} was issued twice");
        }
    }
}

using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

public class BounceBookTests
{
    private static readonly DateOnly Failed = new(2026, 11, 5);
    private static readonly DateOnly Today = new(2026, 11, 6);

    [Theory]
    [InlineData("04", "04")]
    [InlineData("4", "04")]
    [InlineData("NPCI 04 - Balance Insufficient", "04")]
    [InlineData("DG103: retry after 3 days", "DG103")]
    [InlineData("dg 101", "DG101")]
    [InlineData("", null)]
    [InlineData("insufficient funds", null)]
    public void Normalise_FindsTheCodeInWhateverDigioSends(string raw, string? expected) => Assert.Equal(expected, BounceBook.Normalise(raw));

    [Fact]
    public void InsufficientFunds_FirstFailure_IsRepresentedAfterAtLeastThreeDays()
    {
        var d = BounceBook.Decide("04", 1, 2, Failed, Today, representAfterDays: 1);

        Assert.Equal(BounceBook.Represent, d.Action);
        Assert.Equal(Failed.AddDays(3), d.EarliestOn); // a setting below 3 cannot beat DG103
    }

    [Fact]
    public void AttemptsExhausted_Escalates_ByOurLimitOrNpcisMonthlyCount()
    {
        Assert.Equal("AttemptsExhausted", BounceBook.Decide("04", 2, 2, Failed, Today, 3).Kind);
        Assert.Equal("AttemptsExhausted", BounceBook.Decide("06", 1, 5, Failed, Today, 3).Kind); // code 06 allows 1 per month
    }

    [Theory]
    [InlineData("01")]
    [InlineData("61")]
    [InlineData("14")]
    [InlineData("99")]
    [InlineData("zzz")]
    public void NonRpnUnknownOrMissingCodes_NeverRepresent(string code)
    {
        var d = BounceBook.Decide(code, 1, 5, Failed, Today, 3);

        Assert.Equal(BounceBook.Escalate, d.Action);
        Assert.Null(d.EarliestOn);
    }

    [Fact]
    public void DigioRules_Dg103Waits_Dg101AndDg104Escalate()
    {
        Assert.Equal(Failed.AddDays(3), BounceBook.Decide("DG103", 1, 2, Failed, Today, 3).EarliestOn);
        Assert.Equal(BounceBook.Escalate, BounceBook.Decide("DG101", 1, 2, Failed, Today, 3).Action);
        Assert.Equal(BounceBook.Escalate, BounceBook.Decide("DG104", 1, 2, Failed, Today, 3).Action);
    }

    [Fact]
    public void NameOf_GivesPlainWords()
    {
        Assert.Equal("Balance insufficient", BounceBook.NameOf("04"));
        Assert.Null(BounceBook.NameOf("77x"));
    }
}

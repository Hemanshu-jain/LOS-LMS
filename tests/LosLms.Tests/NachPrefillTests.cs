using LosLms.Models;
using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

public sealed class NachPrefillTests
{
    private static readonly BankDetail Bank = new() { Ifsc = "HDFC0001234", BankName = "HDFC Bank", BankBranch = "Pune", AccountNumber = "50100123456789" };

    [Fact]
    public void FillsBlankMandateFromBankAndFirstEmi()
    {
        var m = new EnachMandate();

        NachPrefill.Apply(m, Bank, new DateOnly(2026, 12, 5));

        Assert.Equal("HDFC0001234", m.Ifsc);
        Assert.Equal("HDFC Bank", m.BankName);
        Assert.Equal("50100123456789", m.AccountNumber);
        Assert.Equal("HDFC Bank ····6789", m.LinkedAccount);
        Assert.Equal(new DateOnly(2026, 12, 5), m.DebitDate);
    }

    [Fact]
    public void KeepsWhatTheOfficerEntered()
    {
        var m = new EnachMandate { Ifsc = "SBIN0000001", AccountNumber = "111", LinkedAccount = "Salary a/c", DebitDate = new DateOnly(2026, 12, 10) };

        NachPrefill.Apply(m, Bank, new DateOnly(2026, 12, 5));

        Assert.Equal("SBIN0000001", m.Ifsc);
        Assert.Equal("111", m.AccountNumber);
        Assert.Equal("Salary a/c", m.LinkedAccount);
        Assert.Equal(new DateOnly(2026, 12, 10), m.DebitDate);
    }

    [Fact]
    public void DoesNotTouchAMandateAlreadySentToDigio()
    {
        var m = new EnachMandate { DigioMandateId = "ENA1" };

        NachPrefill.Apply(m, Bank, null);

        Assert.Null(m.AccountNumber);
    }
}

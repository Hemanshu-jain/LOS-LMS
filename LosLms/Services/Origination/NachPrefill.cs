using LosLms.Models;

namespace LosLms.Services;

/// <summary>
/// The E-Nach tab starts from the repayment bank the officer already captured on Bank &amp; Financial, so the account is not typed twice.
/// Blanks only: anything the officer has entered on the mandate is left alone.
/// </summary>
public static class NachPrefill
{
    public static void Apply(EnachMandate m, BankDetail? bank, DateOnly? firstEmiDate)
    {
        if (bank is not null && m.DigioMandateId is null)
        {
            m.Ifsc = Blank(m.Ifsc) ? bank.Ifsc : m.Ifsc;
            m.BankName = Blank(m.BankName) ? bank.BankName : m.BankName;
            m.BankBranch = Blank(m.BankBranch) ? bank.BankBranch : m.BankBranch;
            m.AccountNumber = Blank(m.AccountNumber) ? bank.AccountNumber : m.AccountNumber;
            if (Blank(m.LinkedAccount) && !Blank(bank.AccountNumber))
            {
                var tail = bank.AccountNumber!.Trim();
                m.LinkedAccount = $"{bank.BankName} ····{tail[^Math.Min(4, tail.Length)..]}".Trim();
            }
        }

        m.DebitDate ??= firstEmiDate;
    }

    private static bool Blank(string? s) => string.IsNullOrWhiteSpace(s);
}

using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

/// <summary>When the "bill due" banner shows: from the due date, not in the quiet week before it.</summary>
public sealed class BillReminderTests
{
    private static readonly DateOnly Due = new(2026, 11, 8);

    private static ApiAccess Unpaid(bool allowed = true) => new(allowed, null, Due, 24.90m);

    [Theory]
    [InlineData(2026, 11, 1)]  // the day the October bill is generated
    [InlineData(2026, 11, 2)]
    [InlineData(2026, 11, 7)]  // the day before it is due
    public void BeforeTheDueDate_NoBanner(int year, int month, int day) =>
        Assert.False(Unpaid().ReminderVisible(new DateOnly(year, month, day)));

    [Fact]
    public void OnTheDueDate_TheBannerShows() =>
        Assert.True(Unpaid().ReminderVisible(Due));

    [Fact]
    public void AfterTheDueDate_TheAppIsPaused_SoThePauseScreenCarriesPayNow_NotTheBanner() =>
        Assert.False(Unpaid(allowed: false).ReminderVisible(new DateOnly(2026, 11, 9)));

    [Fact]
    public void NothingDue_NoBanner() =>
        Assert.False(ApiAccess.Open.ReminderVisible(Due));

    [Fact]
    public void EveryDayFromTheDueDate_ThePayPromptShows_AndNeverEarlier()
    {
        // From the due date the customer must always see a way to pay: the banner on the due date itself, and from the
        // next day (when the gate pauses the app) the pause screen's Pay now. Before the due date, neither.
        for (var day = new DateOnly(2026, 11, 1); day <= new DateOnly(2026, 11, 15); day = day.AddDays(1))
        {
            var access = day > Due ? Unpaid(allowed: false) : Unpaid();
            var payPromptShown = access.ReminderVisible(day) || !access.Allowed;
            Assert.Equal(day >= Due, payPromptShown);
        }
    }
}

using LosLms.Models;
using LosLms.Services;
using Xunit;

namespace LosLms.Tests;

/// <summary>Every state the shared Digio badge can show, and when each applies (master plan 2.9).</summary>
public sealed class DigioUiTests
{
    private static readonly DateTime T = new(2026, 10, 27, 10, 0, 0, DateTimeKind.Utc);

    private static DigioRequest Req(string status, string env = "Production") =>
        new() { Id = "r1", Status = status, Environment = env, RequestedAt = T, ErrorMessage = "boom" };

    private static VerificationEvidence Ev(string verdict, string env = "Production", string? fingerprint = "fp1") =>
        new() { Verdict = verdict, Environment = env, InputFingerprint = fingerprint, Summary = "Name match 92", CreatedAt = T };

    private static BadgeInfo Badge(DigioRequest? r = null, VerificationEvidence? e = null, bool configured = true, bool paused = false,
        bool production = true, string? current = "fp1") => DigioUi.Badge(configured, paused, production, r, e, current);

    [Fact]
    public void NotConfigured_BeatsEverything_ThenPaused()
    {
        Assert.Equal(BadgeState.NotConfigured, Badge(Req("Sent"), Ev("Verified"), configured: false, paused: true).State);
        Assert.Equal(BadgeState.Paused, Badge(Req("Sent"), Ev("Verified"), paused: true).State);
    }

    [Fact]
    public void NothingYet_IsNotRun() => Assert.Equal(BadgeState.NotRun, Badge().State);

    [Theory]
    [InlineData("Sending")]
    [InlineData("Sent")]
    [InlineData("Processing")]
    public void ARequestInFlight_IsInProgress_EvenWithOlderEvidence(string status)
    {
        Assert.Equal(BadgeState.InProgress, Badge(Req(status)).State);
        Assert.Equal(BadgeState.InProgress, Badge(Req(status), Ev("Verified")).State); // a re-run is on its way
    }

    [Fact]
    public void WaitingForTheCustomer_IsItsOwnState() =>
        Assert.Equal(BadgeState.WaitingForCustomer, Badge(Req("WaitingForCustomer")).State);

    [Fact]
    public void VerifiedEvidence_ShowsVerified_AndPassesTheGate()
    {
        var b = Badge(e: Ev("Verified"));
        Assert.Equal((BadgeState.Verified, "Verified", true, false), (b.State, b.Label, b.PassesGate, b.Sandbox));
        Assert.Contains("Name match 92", b.Tooltip);
    }

    [Fact]
    public void AnAcceptedMismatch_ReadsVerified_ButSaysSo() =>
        Assert.Equal("Verified (mismatch accepted)", Badge(e: Ev("AcceptedMismatch")).Label);

    [Fact]
    public void EvidenceMadeFromOtherInputs_IsStale_AndDoesNotPass()
    {
        var b = Badge(e: Ev("Verified", fingerprint: "old"), current: "new");
        Assert.Equal((BadgeState.Stale, false), (b.State, b.PassesGate));
    }

    [Fact]
    public void SandboxEvidence_IsTagged_AndNeverPassesOnAProductionCompany()
    {
        var onProduction = Badge(e: Ev("Verified", "Sandbox"), production: true);
        Assert.Equal((BadgeState.Verified, true, false), (onProduction.State, onProduction.Sandbox, onProduction.PassesGate));
        Assert.Contains("production", onProduction.Tooltip);

        var onSandbox = Badge(e: Ev("Verified", "Sandbox"), production: false);
        Assert.True(onSandbox.PassesGate); // a sandbox company testing against sandbox is fine
    }

    [Theory]
    [InlineData("Mismatch", BadgeState.Mismatch, "Mismatch")]
    [InlineData("Failed", BadgeState.Failed, "Failed")]
    [InlineData("Unavailable", BadgeState.Failed, "Unavailable")]
    public void BadVerdicts_NeverPass(string verdict, BadgeState state, string label)
    {
        var b = Badge(e: Ev(verdict));
        Assert.Equal((state, label, false), (b.State, b.Label, b.PassesGate));
    }

    [Theory]
    [InlineData("Expired", BadgeState.Expired)]
    [InlineData("Failed", BadgeState.Failed)]
    [InlineData("Rejected", BadgeState.Failed)]
    [InlineData("Cancelled", BadgeState.Failed)]
    [InlineData("Succeeded", BadgeState.InProgress)] // finished, the evidence is not written yet
    public void FinishedRequestWithoutEvidence_ShowsHowItEnded(string status, BadgeState expected) =>
        Assert.Equal(expected, Badge(Req(status)).State);

    [Fact]
    public void ASandboxRequest_CarriesTheSandboxTag() => Assert.True(Badge(Req("Sent", "Sandbox")).Sandbox);

    [Theory]
    [InlineData(null, "no expiry")]
    [InlineData(-5, "expired")]
    [InlineData(30, "expires in 30 min")]
    [InlineData(150, "expires in 2 h 30 min")]
    [InlineData(60 * 24 * 6 + 60 * 3, "expires in 6 d 3 h")]
    public void Countdown_SaysHowLongIsLeft(int? minutes, string expected) =>
        Assert.Equal(expected, DigioUi.Countdown(minutes is { } m ? T.AddMinutes(m) : null, T));
}

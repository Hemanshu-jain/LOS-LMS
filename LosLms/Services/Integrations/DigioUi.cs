using LosLms.Models;

namespace LosLms.Services;

/// <summary>The honest states a Digio-backed check can be in (master plan 2.9). One list, so no screen invents its own wording.</summary>
public enum BadgeState { NotConfigured, Paused, NotRun, InProgress, WaitingForCustomer, Verified, Mismatch, Failed, Expired, Stale }

/// <param name="Sandbox">Show the SANDBOX tag: the result came from Digio's test environment.</param>
/// <param name="PassesGate">Verified, fresh, and not sandbox evidence on a production company. What a gate may rely on.</param>
public sealed record BadgeInfo(BadgeState State, string Label, string Css, bool Sandbox, bool PassesGate, string? Tooltip);

/// <summary>Maps a request and its evidence to the one badge every screen shows.</summary>
public static class DigioUi
{
    /// <param name="configured">The company has Digio keys and this product switched on.</param>
    /// <param name="paused">API access is paused for an unpaid bill.</param>
    /// <param name="companyProduction">The company's keys are the production ones (sandbox evidence then never counts).</param>
    /// <param name="request">The newest request for this check, if any.</param>
    /// <param name="evidence">The newest evidence for this check, if any.</param>
    /// <param name="currentFingerprint">Fingerprint of the inputs as they are now; evidence made from other inputs is Stale.</param>
    public static BadgeInfo Badge(
        bool configured, bool paused, bool companyProduction,
        DigioRequest? request, VerificationEvidence? evidence, string? currentFingerprint)
    {
        if (!configured)
        {
            return Info(BadgeState.NotConfigured, "Not configured", "off", false, false, "Digio is not set up for this check.");
        }

        if (paused)
        {
            return Info(BadgeState.Paused, "Paused (bill)", "warn", false, false, "Digio calls are paused until the unpaid bill is settled.");
        }

        // A check in flight wins over an older result: the officer re-ran it and the new answer is on its way.
        if (request is not null)
        {
            var sandbox = request.Environment == "Sandbox";
            switch (request.Status)
            {
                case "Sending" or "Sent" or "Processing":
                    return Info(BadgeState.InProgress, "In progress", "info", sandbox, false, When("Started", request.RequestedAt));
                case "WaitingForCustomer":
                    return Info(BadgeState.WaitingForCustomer, "Waiting for customer", "info", sandbox, false, When("Sent", request.RequestedAt));
            }
        }

        if (evidence is not null)
        {
            var sandbox = evidence.Environment == "Sandbox";
            var tip = string.Join(" · ", new[] { evidence.Summary, When("Checked", evidence.CreatedAt) }.Where(s => !string.IsNullOrWhiteSpace(s)));
            switch (evidence.Verdict)
            {
                case "Verified" or "AcceptedMismatch":
                    if (evidence.IsStaleFor(currentFingerprint))
                    {
                        return Info(BadgeState.Stale, "Stale", "warn", sandbox, false, "The details changed after this check. Run it again. " + tip);
                    }

                    var accepted = evidence.Verdict == "AcceptedMismatch";
                    var passes = !(sandbox && companyProduction);
                    return Info(BadgeState.Verified, accepted ? "Verified (mismatch accepted)" : "Verified", "ok", sandbox, passes,
                        sandbox && companyProduction ? "Sandbox result: does not count on a production company. " + tip : tip);
                case "Mismatch":
                    return Info(BadgeState.Mismatch, "Mismatch", "bad", sandbox, false, tip);
                default: // Failed, Unavailable
                    return Info(BadgeState.Failed, evidence.Verdict == "Unavailable" ? "Unavailable" : "Failed", "bad", sandbox, false, tip);
            }
        }

        if (request is not null)
        {
            var sandbox = request.Environment == "Sandbox";
            return request.Status switch
            {
                "Expired" => Info(BadgeState.Expired, "Expired", "warn", sandbox, false, "The customer did not finish in time. Start a new request."),
                "Failed" or "Rejected" or "Cancelled" => Info(BadgeState.Failed, "Failed", "bad", sandbox, false, request.ErrorMessage),
                _ => Info(BadgeState.InProgress, "Finishing", "info", sandbox, false, When("Done", request.CompletedAt ?? request.RequestedAt)), // Succeeded, evidence not written yet
            };
        }

        return Info(BadgeState.NotRun, "Not run", "off", false, false, null);
    }

    /// <summary>"expires in 6 d 3 h", "expires in 45 min", "expired" or "no expiry".</summary>
    public static string Countdown(DateTime? expiresAt, DateTime nowUtc)
    {
        if (expiresAt is null)
        {
            return "no expiry";
        }

        var left = expiresAt.Value - nowUtc;
        return left <= TimeSpan.Zero ? "expired"
            : left.TotalDays >= 1 ? $"expires in {(int)left.TotalDays} d {left.Hours} h"
            : left.TotalHours >= 1 ? $"expires in {(int)left.TotalHours} h {left.Minutes} min"
            : $"expires in {Math.Max(1, (int)left.TotalMinutes)} min";
    }

    private static BadgeInfo Info(BadgeState state, string label, string css, bool sandbox, bool passes, string? tooltip) =>
        new(state, label, css, sandbox, passes, string.IsNullOrWhiteSpace(tooltip) ? null : tooltip.Trim());

    private static string When(string what, DateTime utc) => $"{what} {utc:d MMM HH:mm} UTC";
}

/// <summary>
/// Part 2 (the webhook processor and reconciler) supplies the real one. The return page asks it to look at a request right away
/// instead of waiting for the next poll; until then nothing happens and the poll finds it anyway.
/// </summary>
public interface IDigioReconcileTrigger
{
    Task RequestReconcileAsync(string requestId);
}

public sealed class NoopDigioReconcileTrigger : IDigioReconcileTrigger
{
    public Task RequestReconcileAsync(string requestId) => Task.CompletedTask;
}

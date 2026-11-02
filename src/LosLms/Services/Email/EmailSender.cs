using System.Net;
using System.Net.Mail;

namespace LosLms.Services;

/// <summary>Sends one email. Never throws: a mail problem must not break sign-up or a password reset.</summary>
public interface IEmailSender
{
    /// <summary>False until <c>Smtp__Host</c> and <c>Smtp__From</c> are set.</summary>
    bool IsConfigured { get; }

    /// <returns>True when the message was handed to the mail server; false when not configured or the send failed.</returns>
    Task<bool> SendAsync(string to, string subject, string html, string text);
}

/// <summary>
/// SMTP over STARTTLS with System.Net.Mail, configured from <c>Smtp__Host/Port/User/Password/From/FromName</c> (values
/// trimmed). Production relays through Brevo on port 2525 because Render blocks 25/465/587. One attempt, logged on failure;
/// with no SMTP configured the email (link included) is logged at Information instead, so development and tests work.
/// </summary>
public sealed class SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> log) : IEmailSender
{
    public bool IsConfigured => config.Trimmed("Smtp:Host") is not null && config.Trimmed("Smtp:From") is not null;

    public async Task<bool> SendAsync(string to, string subject, string html, string text)
    {
        if (!IsConfigured)
        {
            log.LogInformation("SMTP is not configured, so this email was not sent. To: {To} | Subject: {Subject}\n{Text}", to, subject, text);
            return false;
        }

        try
        {
            using var message = new MailMessage { Subject = subject, Body = text, IsBodyHtml = false };
            message.From = new MailAddress(config.Trimmed("Smtp:From")!, config.Trimmed("Smtp:FromName") ?? "LOS/LMS");
            message.To.Add(to);
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, "text/html"));

            var port = int.TryParse(config.Trimmed("Smtp:Port"), out var p) ? p : 587;
            using var client = new SmtpClient(config.Trimmed("Smtp:Host")!, port)
            {
                // STARTTLS on 587/2525. Only a local mail catcher (MailHog etc.) needs Smtp__EnableSsl=false; never set it for a real relay.
                EnableSsl = !string.Equals(config.Trimmed("Smtp:EnableSsl"), "false", StringComparison.OrdinalIgnoreCase),
                Timeout = 15_000,
            };
            if (config.Trimmed("Smtp:User") is { } user)
            {
                client.Credentials = new NetworkCredential(user, config.Trimmed("Smtp:Password") ?? string.Empty);
            }

            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            // The message text (which holds a one-time link) and the credentials are deliberately not logged.
            log.LogWarning("Email to {To} could not be sent: {Error}", Mask(to), ex.GetType().Name + ": " + (ex.InnerException?.Message ?? ex.Message));
            return false;
        }
    }

    private static string Mask(string address)
    {
        var at = address.IndexOf('@');
        return at < 1 ? "***" : address[0] + "***" + address[at..];
    }
}

/// <summary>
/// Caps how many emails one address can be sent per hour, so the public "resend" and "forgot password" forms can't be
/// used to flood someone's inbox. In memory (one server); the per-IP request limiter is separate.
/// </summary>
public sealed class SendThrottle
{
    private static readonly TimeSpan Window = TimeSpan.FromHours(1);
    private const int MaxKeys = 10_000;

    private readonly Dictionary<string, List<DateTime>> _sent = [];
    private readonly object _gate = new();

    /// <summary>True (and counts it) when <paramref name="key"/> may be sent one more email within the window.</summary>
    public bool Allow(string key, DateTime now, int max = 3)
    {
        lock (_gate)
        {
            var times = _sent.TryGetValue(key, out var t) ? t.Where(x => now - x < Window).ToList() : [];
            if (times.Count >= max)
            {
                return false;
            }

            times.Add(now);
            _sent.Remove(key);
            if (_sent.Count >= MaxKeys)
            {
                _sent.Remove(_sent.Keys.First()); // oldest first
            }

            _sent[key] = times;
            return true;
        }
    }
}

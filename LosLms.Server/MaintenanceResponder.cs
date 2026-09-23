using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LosLms.Server;

/// <summary>
/// A tiny stand-in HTTP responder the launcher runs on the backend's loopback port WHILE the backend is
/// stopped (most usefully across an update file-swap). The Cloudflare tunnel connects to
/// <c>localhost:5037</c>; with nothing listening there it would get connection-refused and Cloudflare
/// would paint its own raw "origin is unreachable" (Error 1033 / 5xx) page. This keeps something on the
/// port that answers every request with our own branded "we'll be right back" page instead.
/// </summary>
/// <remarks>
/// Deliberately a raw <see cref="TcpListener"/> writing one fixed HTTP/1.1 response, not a Kestrel/YARP
/// reverse proxy: this lives in the never-swapped launcher, where the priority is that it always works
/// and adds no framework surface. It does NOT proxy — it is only ever up when the backend is down, and
/// <see cref="BackendSupervisor.StartBackend"/> stops it before the backend re-binds the port.
///
/// Scope, stated honestly: it covers the swap window (the longest part of an update). It does NOT cover
/// the seconds while the freshly-started backend is still binding, nor a cold first-boot — the port
/// cannot be held by both at once, so a brief Cloudflare error is still possible right as the backend
/// comes back. Closing that gap entirely would need a permanent reverse proxy (a much larger change).
///
/// The HTML is embedded here rather than read from disk because during an update the app folder (with
/// wwwroot/maintenance.html) is moved aside — the launcher must not depend on it.
/// </remarks>
internal sealed class MaintenanceResponder
{
    private readonly int _port;
    private readonly byte[] _response;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public MaintenanceResponder(int port)
    {
        _port = port;

        var body = Encoding.UTF8.GetBytes(Html);
        var header =
            "HTTP/1.1 503 Service Unavailable\r\n"
            + "Content-Type: text/html; charset=utf-8\r\n"
            + $"Content-Length: {body.Length}\r\n"
            + "Retry-After: 20\r\n"
            + "Cache-Control: no-store\r\n"
            + "Connection: close\r\n\r\n";
        _response = [.. Encoding.ASCII.GetBytes(header), .. body];
    }

    /// <summary>Bind the loopback port and start answering. Idempotent; best-effort (logs and gives up on error).</summary>
    public void Start()
    {
        if (_listener is not null)
        {
            return;
        }

        try
        {
            var listener = new TcpListener(IPAddress.Loopback, _port);
            // Bind through a lingering TIME_WAIT from the backend socket we are replacing.
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();

            _listener = listener;
            _cts = new CancellationTokenSource();
            _ = AcceptLoopAsync(listener, _cts.Token);
            Log.Info($"Maintenance page is serving on 127.0.0.1:{_port} while the backend is down.");
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not start the maintenance responder: {ex.Message}");
            _listener = null;
        }
    }

    /// <summary>Stop answering and release the port so the backend can re-bind it. Idempotent.</summary>
    public void Stop()
    {
        if (_listener is null)
        {
            return;
        }

        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _listener.Stop(); } catch { /* ignore */ }
        _listener = null;
        _cts = null;
        Log.Info("Maintenance responder stopped; the port is free for the backend.");
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(ct);
            }
            catch
            {
                break; // listener stopped or cancelled
            }

            _ = RespondAsync(client, ct);
        }
    }

    private async Task RespondAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        {
            try
            {
                client.NoDelay = true;
                var stream = client.GetStream();

                // Read (and discard) whatever request arrived, so the peer is ready to read our reply.
                // A short read is enough — we answer every request the same way regardless of content.
                var scratch = new byte[2048];
                using var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                readCts.CancelAfter(250);
                try { await stream.ReadAsync(scratch, readCts.Token); } catch { /* fine — respond anyway */ }

                await stream.WriteAsync(_response, ct);
                await stream.FlushAsync(ct);
            }
            catch
            {
                // Best effort: a dropped connection during downtime is not worth surfacing.
            }
        }
    }

    // Self-contained, mirrors wwwroot/maintenance.html (flat, navy, hard-edged). No external assets.
    private const string Html =
        "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">"
        + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">"
        + "<title>Under maintenance · LOS/LMS</title><style>"
        + "*{box-sizing:border-box}"
        + "body{margin:0;min-height:100vh;display:flex;align-items:center;justify-content:center;padding:24px;"
        + "background:#eceef1;color:#1a1f29;font-family:'IBM Plex Sans',system-ui,-apple-system,Segoe UI,Roboto,sans-serif}"
        + ".card{width:100%;max-width:400px;background:#fff;border:1px solid #d8dbe1;padding:30px 30px 28px;"
        + "box-shadow:0 18px 48px rgba(26,31,41,.12)}"
        + ".eyebrow{display:block;font-size:10px;font-weight:600;letter-spacing:.16em;text-transform:uppercase;color:#8d8d8d}"
        + ".icon{width:34px;height:34px;margin:16px 0 10px;color:#1f3a5f}"
        + "h1{margin:0 0 8px;font-size:22px;font-weight:600;line-height:1.15}"
        + "p{margin:0 0 12px;font-size:13px;line-height:1.55;color:#5b6472}p:last-child{margin-bottom:0}"
        + ".rule{height:1px;background:#d8dbe1;margin:20px 0 16px}.note{font-size:11px;color:#8d8d8d}"
        + "</style></head><body><main class=\"card\">"
        + "<span class=\"eyebrow\">LOS/LMS</span>"
        + "<svg class=\"icon\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" "
        + "stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">"
        + "<path d=\"M14.7 6.3a4 4 0 0 0-5.4 5.4l-6 6a1.5 1.5 0 0 0 2.1 2.1l6-6a4 4 0 0 0 5.4-5.4l-2.5 2.5-2.1-2.1 2.5-2.5z\"/>"
        + "</svg>"
        + "<h1>We'll be right back</h1>"
        + "<p>LOS/LMS is briefly offline while we apply an update. Nothing is wrong on your end, and your "
        + "data is safe — this usually takes under a minute.</p>"
        + "<p>Please refresh this page shortly. Thank you for your patience.</p>"
        + "<div class=\"rule\"></div>"
        + "<p class=\"note\">If this continues longer than expected, contact your system administrator.</p>"
        + "</main></body></html>";
}

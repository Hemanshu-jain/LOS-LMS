using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LosLms.Server;

/// <summary>
/// Runs a Cloudflare Quick Tunnel (no account, no domain) that exposes the local backend at a
/// generated <c>https://xxxxx.trycloudflare.com</c> URL, and captures that URL from cloudflared's
/// output. The tunnel is purely additive: if it cannot be established the launcher carries on and the
/// machine's own operator keeps working locally.
/// </summary>
internal sealed partial class TunnelManager
{
    private const string DownloadUrl =
        "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe";

    private static readonly TimeSpan UrlTimeout = TimeSpan.FromSeconds(45);

    private Process? _cloudflared;

    /// <summary>The captured public URL, or null if the tunnel is not up.</summary>
    public string? PublicUrl { get; private set; }

    /// <summary>
    /// Ensures cloudflared is present (bundled, or downloaded and cached on first run), starts it
    /// windowless against the backend port, and waits for the trycloudflare URL. Returns null on any
    /// failure — the caller treats that as "local only", never as fatal.
    /// </summary>
    public async Task<string?> StartAsync(int backendPort, Action<string> progress, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(Paths.CloudflaredExe))
            {
                progress("Downloading the tunnel client…");
                await DownloadCloudflaredAsync(ct);
            }

            progress("Opening the shareable tunnel…");
            PublicUrl = await StartAndCaptureUrlAsync(backendPort, ct);

            if (PublicUrl is null)
            {
                Log.Warn("Tunnel started but no trycloudflare URL was captured in time. Continuing local-only.");
            }
            else
            {
                Log.Info($"Tunnel URL: {PublicUrl}");
            }

            return PublicUrl;
        }
        catch (Exception ex)
        {
            Log.Error("Could not establish the Cloudflare tunnel. Continuing local-only.", ex);
            Stop();
            return null;
        }
    }

    public void Stop()
    {
        try
        {
            if (_cloudflared is { HasExited: false })
            {
                _cloudflared.Kill(entireProcessTree: true);
                _cloudflared.WaitForExit(10_000);
                Log.Info("Tunnel stopped.");
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Error stopping the tunnel: {ex.Message}");
        }
    }

    private async Task<string?> StartAndCaptureUrlAsync(int backendPort, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        var info = new ProcessStartInfo
        {
            FileName = Paths.CloudflaredExe,
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        info.ArgumentList.Add("tunnel");
        info.ArgumentList.Add("--url");
        info.ArgumentList.Add($"http://127.0.0.1:{backendPort}");
        info.ArgumentList.Add("--no-autoupdate");
        // Force the TCP-based http2 edge protocol instead of the default QUIC (UDP 7844). Quick Tunnels
        // over QUIC drop frequently on networks that throttle or block UDP; http2 is markedly more
        // stable for a long-lived tunnel. cloudflared still logs the trycloudflare URL to stderr, which
        // is what we scrape below.
        info.ArgumentList.Add("--protocol");
        info.ArgumentList.Add("http2");

        void OnData(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            var match = TryCloudflareUrl().Match(line);
            if (match.Success)
            {
                tcs.TrySetResult(match.Value);
            }
        }

        _cloudflared = new Process { StartInfo = info, EnableRaisingEvents = true };
        _cloudflared.ErrorDataReceived += (_, e) => OnData(e.Data);
        _cloudflared.OutputDataReceived += (_, e) => OnData(e.Data);

        if (!_cloudflared.Start())
        {
            throw new InvalidOperationException("Could not start cloudflared.");
        }

        _cloudflared.BeginErrorReadLine();
        _cloudflared.BeginOutputReadLine();

        using var timeout = new CancellationTokenSource(UrlTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
        using var _ = linked.Token.Register(() => tcs.TrySetResult(string.Empty));

        var url = await tcs.Task;
        return string.IsNullOrEmpty(url) ? null : url;
    }

    private static async Task DownloadCloudflaredAsync(CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        await using var stream = await http.GetStreamAsync(DownloadUrl, ct);
        var temp = Paths.CloudflaredExe + ".part";
        await using (var file = File.Create(temp))
        {
            await stream.CopyToAsync(file, ct);
        }

        File.Move(temp, Paths.CloudflaredExe, overwrite: true);
        Log.Info("cloudflared downloaded and cached.");
    }

    [GeneratedRegex(@"https://[a-z0-9-]+\.trycloudflare\.com")]
    private static partial Regex TryCloudflareUrl();
}

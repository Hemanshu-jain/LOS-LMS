using System.Diagnostics;

namespace LosLms.Server;

/// <summary>
/// Runs the named Cloudflare tunnel that exposes the local backend at the fixed public URL
/// (e.g. https://los-lms.bhodhix.com). Unlike a Quick Tunnel, the hostname is permanent and the
/// hostname → localhost:port mapping is configured once in the Cloudflare Zero Trust dashboard; here we
/// only run <c>cloudflared tunnel run --token &lt;token&gt;</c>. The tunnel is additive: if it cannot
/// start, the operator at this machine keeps working locally.
/// </summary>
internal sealed class TunnelManager
{
    private const string DownloadUrl =
        "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe";

    private Process? _cloudflared;

    /// <summary>
    /// Ensures cloudflared is present (bundled, or downloaded and cached on first run) and starts it
    /// windowless against the configured named tunnel. Returns true when the process started. The public
    /// URL is fixed and known from config, so there is nothing to capture. Never throws — a failure is
    /// logged and treated as "local only".
    /// </summary>
    public async Task<bool> StartAsync(string tunnelToken, Action<string> progress, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(Paths.CloudflaredExe))
            {
                progress("Downloading the tunnel client…");
                await DownloadCloudflaredAsync(ct);
            }

            progress("Connecting the tunnel…");

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
            info.ArgumentList.Add("run");
            info.ArgumentList.Add("--token");
            info.ArgumentList.Add(tunnelToken);
            // Force the TCP-based http2 edge protocol instead of the default QUIC (UDP 7844): http2 is
            // markedly more stable on networks that throttle or block UDP.
            info.ArgumentList.Add("--protocol");
            info.ArgumentList.Add("http2");
            info.ArgumentList.Add("--no-autoupdate");

            _cloudflared = new Process { StartInfo = info, EnableRaisingEvents = true };
            _cloudflared.ErrorDataReceived += (_, e) => LogLine(e.Data);
            _cloudflared.OutputDataReceived += (_, e) => LogLine(e.Data);

            if (!_cloudflared.Start())
            {
                throw new InvalidOperationException("Could not start cloudflared.");
            }

            _cloudflared.BeginErrorReadLine();
            _cloudflared.BeginOutputReadLine();

            Log.Info("Named tunnel starting (cloudflared launched). Remote users reach the fixed hosted URL.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("Could not start the Cloudflare tunnel. Continuing local-only.", ex);
            Stop();
            return false;
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

    // cloudflared is chatty; keep its lines in the launcher log at info level for diagnosis.
    private static void LogLine(string? line)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            Log.Info($"[tunnel] {line}");
        }
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
}

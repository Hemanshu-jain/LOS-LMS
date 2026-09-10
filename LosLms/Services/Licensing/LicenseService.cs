using System.Text.Json;
using LosLms.Licensing;

namespace LosLms.Services;

public enum LicenseState
{
    /// <summary>No license baked or stored (e.g. a dev build) — enforcement is off.</summary>
    Unlicensed,
    Licensed,
    ExpiringSoon,
    Expired,
}

/// <summary>
/// Loads and enforces the app's license. The active license is the renewal key the SuperAdmin last
/// entered (if it verifies and extends past the baked one), otherwise the license baked into the build.
/// The app freezes only once a real license is present AND past its expiry — a dev build with no license
/// is never frozen. Clock-rollback is defended by remembering the latest time ever seen and never letting
/// "now" fall below it.
/// </summary>
public sealed class LicenseService : IDisposable
{
    private static readonly TimeSpan WarnWindow = TimeSpan.FromDays(14);
    private static readonly TimeSpan ClockPersistStep = TimeSpan.FromHours(1);

    private readonly string _stateFile;
    private readonly object _gate = new();
    private readonly Timer _clockTimer;

    private License? _license;
    private string? _token;
    private DateTimeOffset _maxSeen;

    public LicenseService(IHostEnvironment env)
    {
        _stateFile = Path.Combine(env.ContentRootPath, "App_Data", "license.json");
        Load();
        // Advance the anti-rollback clock hourly (not on every request), so file IO stays trivial.
        _clockTimer = new Timer(_ => AdvanceClock(), null, ClockPersistStep, ClockPersistStep);
    }

    /// <summary>Current state, computed cheaply on demand (no IO).</summary>
    public LicenseState State
    {
        get
        {
            lock (_gate)
            {
                if (_license is null)
                {
                    return LicenseState.Unlicensed;
                }

                var remaining = _license.ExpiresUtc - EffectiveNow();
                if (remaining <= TimeSpan.Zero)
                {
                    return LicenseState.Expired;
                }

                return remaining <= WarnWindow ? LicenseState.ExpiringSoon : LicenseState.Licensed;
            }
        }
    }

    public bool IsFrozen => State == LicenseState.Expired;

    public string? Client { get { lock (_gate) { return _license?.Client; } } }

    public DateTimeOffset? ExpiresUtc { get { lock (_gate) { return _license?.ExpiresUtc; } } }

    public int DaysRemaining
    {
        get
        {
            lock (_gate)
            {
                if (_license is null)
                {
                    return 0;
                }

                return Math.Max(0, (int)Math.Ceiling((_license.ExpiresUtc - EffectiveNow()).TotalDays));
            }
        }
    }

    /// <summary>Applies a pasted renewal key. Rejects a key for a different install or one that doesn't extend.</summary>
    public (bool Ok, string Message) ApplyRenewal(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !LicenseKeys.TryVerify(token.Trim(), out var lic) || lic is null)
        {
            return (false, "That renewal key is not valid.");
        }

        lock (_gate)
        {
            if (_license is not null && !string.Equals(lic.Host, _license.Host, StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"That key is for {lic.Host}, not this installation.");
            }

            if (_license is not null && lic.ExpiresUtc <= _license.ExpiresUtc)
            {
                return (false, "That key does not extend the current licence — it may be an old one.");
            }

            _license = lic;
            _token = token.Trim();
            Persist();
        }

        return (true, $"Renewed — active until {lic.ExpiresUtc:d MMM yyyy}.");
    }

    private DateTimeOffset EffectiveNow()
    {
        var now = DateTimeOffset.UtcNow;
        return now > _maxSeen ? now : _maxSeen;
    }

    private void AdvanceClock()
    {
        lock (_gate)
        {
            var now = DateTimeOffset.UtcNow;
            if (now - _maxSeen > ClockPersistStep)
            {
                _maxSeen = now;
                Persist();
            }
        }
    }

    private void Load()
    {
        Stored? stored = null;
        try
        {
            if (File.Exists(_stateFile))
            {
                stored = JsonSerializer.Deserialize<Stored>(File.ReadAllText(_stateFile));
            }
        }
        catch
        {
            // ignore — fall back to the baked license
        }

        _maxSeen = stored?.MaxSeenUtc ?? DateTimeOffset.MinValue;

        License? baked = null;
        var bakedToken = LicenseKeys.BakedToken;
        if (bakedToken is not null)
        {
            LicenseKeys.TryVerify(bakedToken, out baked);
        }

        License? renewal = null;
        if (!string.IsNullOrWhiteSpace(stored?.Token))
        {
            LicenseKeys.TryVerify(stored.Token, out renewal);
        }

        // The renewal wins only if it verifies AND extends past the baked expiry.
        if (renewal is not null && (baked is null || renewal.ExpiresUtc > baked.ExpiresUtc))
        {
            _license = renewal;
            _token = stored!.Token;
        }
        else
        {
            _license = baked;
            _token = bakedToken;
        }
    }

    private void Persist()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_stateFile)!);
            File.WriteAllText(_stateFile, JsonSerializer.Serialize(new Stored { Token = _token, MaxSeenUtc = _maxSeen }));
        }
        catch
        {
            // best effort — a read-only disk shouldn't crash the app
        }
    }

    public void Dispose() => _clockTimer.Dispose();

    private sealed class Stored
    {
        public string? Token { get; set; }
        public DateTimeOffset MaxSeenUtc { get; set; }
    }
}

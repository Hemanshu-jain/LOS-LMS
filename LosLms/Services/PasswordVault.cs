using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LosLms.Services;

/// <summary>
/// A server-side vault of admin-set passwords, so an Admin can re-view a staff (or their own) credential
/// later — masked in the UI, revealed briefly on demand. This is a DELIBERATE, accepted trade-off: it
/// keeps passwords in a recoverable form, which a pure one-way hash would not.
/// </summary>
/// <remarks>
/// The recoverable copy is kept OUT of the main database on purpose. It lives in two files under
/// <c>App_Data</c>: a 256-bit key (<c>vault.key</c>) and the encrypted entries (<c>credential-vault.dat</c>,
/// AES-256-GCM, one blob per user id). A database dump alone therefore cannot expose any password — an
/// attacker would also need the server's key file. Identity's hash is still the source of truth for
/// authentication; this vault is only ever read for the Admin's reveal.
///
/// Single-server only, like the rest of the deployment. Registered as a singleton; every method locks,
/// so concurrent circuits do not corrupt the store.
/// </remarks>
public sealed class PasswordVault
{
    private const int NonceSize = 12; // AES-GCM standard nonce
    private const int TagSize = 16;   // AES-GCM standard tag

    private readonly object _lock = new();
    private readonly string _keyPath;
    private readonly string _storePath;
    private readonly byte[] _key;

    public PasswordVault(IWebHostEnvironment environment)
    {
        var dir = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dir);
        _keyPath = Path.Combine(dir, "vault.key");
        _storePath = Path.Combine(dir, "credential-vault.dat");
        _key = LoadOrCreateKey();
    }

    /// <summary>Stores (or replaces) the password for a user, encrypted at rest.</summary>
    public void Store(string userId, string password)
    {
        lock (_lock)
        {
            var store = Read();
            store[userId] = Encrypt(password);
            Write(store);
        }
    }

    /// <summary>The stored password for a user, or null when none was captured (e.g. an older account).</summary>
    public string? TryReveal(string userId)
    {
        lock (_lock)
        {
            var store = Read();
            if (!store.TryGetValue(userId, out var blob)) { return null; }
            try { return Decrypt(blob); }
            catch { return null; } // key rotated / entry corrupt — treat as unavailable, never throw
        }
    }

    /// <summary>Whether a viewable password exists for the user.</summary>
    public bool Has(string userId)
    {
        lock (_lock) { return Read().ContainsKey(userId); }
    }

    private byte[] LoadOrCreateKey()
    {
        if (File.Exists(_keyPath))
        {
            var existing = File.ReadAllBytes(_keyPath);
            if (existing.Length == 32) { return existing; }
        }

        var key = RandomNumberGenerator.GetBytes(32);
        File.WriteAllBytes(_keyPath, key);
        return key;
    }

    private Dictionary<string, string> Read()
    {
        if (!File.Exists(_storePath)) { return new Dictionary<string, string>(StringComparer.Ordinal); }
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_storePath))
                ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    private void Write(Dictionary<string, string> store) =>
        File.WriteAllText(_storePath, JsonSerializer.Serialize(store));

    private string Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        // Layout: nonce | tag | ciphertext, base64 for JSON storage.
        var combined = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, combined, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, combined, NonceSize + TagSize, cipher.Length);
        return Convert.ToBase64String(combined);
    }

    private string Decrypt(string blob)
    {
        var combined = Convert.FromBase64String(blob);
        var nonce = combined.AsSpan(0, NonceSize);
        var tag = combined.AsSpan(NonceSize, TagSize);
        var cipher = combined.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}

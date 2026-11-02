using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LosLms.Data;
using LosLms.Models;
using Microsoft.EntityFrameworkCore;

namespace LosLms.Services;

/// <summary>
/// A server-side vault of admin-set passwords, so an Admin can re-view a staff (or their own) credential
/// later — masked in the UI, revealed briefly on demand. This is a DELIBERATE, accepted trade-off: it
/// keeps passwords in a recoverable form, which a pure one-way hash would not.
/// </summary>
/// <remarks>
/// The entries are AES-256-GCM blobs in the <c>CredentialVault</c> table, one per user id. The key is NOT in
/// the database: it comes from the <c>Vault__Key</c> setting (cloud) or, for a local run with none set, from
/// <c>App_Data/vault.key</c>. A database dump alone therefore cannot expose any password — an attacker also
/// needs the key. Identity's hash is still the source of truth for authentication; this vault is only ever
/// read for the Admin's reveal.
///
/// Registered as a singleton; every method locks, so concurrent circuits do not interleave a read-modify-write.
/// </remarks>
public sealed class PasswordVault
{
    private const int NonceSize = 12; // AES-GCM standard nonce
    private const int TagSize = 16;   // AES-GCM standard tag

    private readonly object _lock = new();
    private readonly DbContextOptions<LosDbContext> _options;
    private readonly byte[] _key;

    public PasswordVault(DbContextOptions<LosDbContext> options, IConfiguration config, IWebHostEnvironment environment)
    {
        _options = options;
        _key = KeyFromSetting(config.Trimmed("Vault:Key")) ?? LoadOrCreateFileKey(environment.ContentRootPath);
    }

    /// <summary>Stores (or replaces) the password for a user, encrypted at rest.</summary>
    public void Store(string userId, string password)
    {
        lock (_lock)
        {
            using var db = NewContext();
            Upsert(db, userId, Encrypt(password));
            db.SaveChanges();
        }
    }

    /// <summary>The stored password for a user, or null when none was captured (e.g. an older account).</summary>
    public string? TryReveal(string userId)
    {
        lock (_lock)
        {
            using var db = NewContext();
            var blob = db.CredentialVault.AsNoTracking().Where(e => e.UserId == userId).Select(e => e.Blob).FirstOrDefault();
            if (blob is null) { return null; }
            try { return Decrypt(blob); }
            catch { return null; } // key rotated / entry corrupt — treat as unavailable, never throw
        }
    }

    /// <summary>Whether a viewable password exists for the user.</summary>
    public bool Has(string userId)
    {
        lock (_lock)
        {
            using var db = NewContext();
            return db.CredentialVault.Any(e => e.UserId == userId);
        }
    }

    /// <summary>
    /// One-time move of the old file vault (<c>vault.key</c> + <c>credential-vault.dat</c> from a desktop
    /// install's App_Data) into the table, re-encrypted under the current key. Run it with
    /// <c>--import-vault &lt;folder&gt;</c>. Entries that already exist are kept; returns how many were imported.
    /// </summary>
    public int ImportLegacyFiles(string folder)
    {
        var oldKey = File.ReadAllBytes(Path.Combine(folder, "vault.key"));
        if (oldKey.Length != 32) { throw new InvalidDataException("vault.key must be exactly 32 bytes."); }

        var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(folder, "credential-vault.dat"))) ?? new();

        lock (_lock)
        {
            using var db = NewContext();
            var imported = 0;
            foreach (var (userId, blob) in entries)
            {
                if (db.CredentialVault.Any(e => e.UserId == userId)) { continue; }
                string plain;
                try { plain = Decrypt(blob, oldKey); }
                catch { continue; } // an entry the old key cannot open is skipped, not fatal
                Upsert(db, userId, Encrypt(plain));
                imported++;
            }

            db.SaveChanges();
            return imported;
        }
    }

    private LosDbContext NewContext() => new(_options, TenantContext.ForSeeding());

    private static void Upsert(LosDbContext db, string userId, string blob)
    {
        var row = db.CredentialVault.Find(userId);
        if (row is null)
        {
            db.CredentialVault.Add(new CredentialVaultEntry { UserId = userId, Blob = blob });
        }
        else
        {
            row.Blob = blob;
            row.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>A 32-byte base64 value is used as-is; any other non-empty text is stretched with SHA-256.</summary>
    private static byte[]? KeyFromSetting(string? value)
    {
        if (value is null) { return null; }
        try
        {
            var raw = Convert.FromBase64String(value);
            if (raw.Length == 32) { return raw; }
        }
        catch (FormatException) { /* not base64: fall through */ }

        return SHA256.HashData(Encoding.UTF8.GetBytes(value));
    }

    // Local runs only (no Vault__Key): a key file next to the data, as before. On a host whose disk is wiped
    // at every restart this would silently lose every entry, which is why the cloud docs require Vault__Key.
    private static byte[] LoadOrCreateFileKey(string contentRoot)
    {
        var dir = Path.Combine(contentRoot, "App_Data");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "vault.key");
        if (File.Exists(path))
        {
            var existing = File.ReadAllBytes(path);
            if (existing.Length == 32) { return existing; }
        }

        var key = RandomNumberGenerator.GetBytes(32);
        File.WriteAllBytes(path, key);
        return key;
    }

    /// <summary>Encrypts arbitrary text with the vault key (AES-256-GCM, nonce|tag|ciphertext, base64): for payloads that hold personal data.</summary>
    public string Seal(string plaintext) => Encrypt(plaintext);

    /// <summary>Reverses <see cref="Seal"/>; throws if the text was not sealed with this key.</summary>
    public string Unseal(string sealedText) => Decrypt(sealedText);

    private string Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        // Layout: nonce | tag | ciphertext, base64 for storage.
        var combined = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, combined, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, combined, NonceSize + TagSize, cipher.Length);
        return Convert.ToBase64String(combined);
    }

    private string Decrypt(string blob, byte[]? key = null)
    {
        var combined = Convert.FromBase64String(blob);
        var nonce = combined.AsSpan(0, NonceSize);
        var tag = combined.AsSpan(NonceSize, TagSize);
        var cipher = combined.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(key ?? _key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}

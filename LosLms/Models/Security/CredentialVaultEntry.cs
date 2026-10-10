using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// One encrypted credential in the vault (see <c>PasswordVault</c>). The blob is AES-256-GCM under a key that
/// is NOT in the database (env <c>Vault__Key</c>), so a database dump alone exposes no password. Deliberately
/// not tenant-filtered: the vault is a vendor-level store read only after the caller's own authorisation.
/// </summary>
public class CredentialVaultEntry
{
    [Key]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>nonce | tag | ciphertext, base64.</summary>
    public string Blob { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

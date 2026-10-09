using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

// "Connect to LOS-LMS": partner desktop software (Paavan) links to a company with a device code the
// company's Admin approves at /link. Every code and token is stored as SHA-256 hex only.
// Desktop (public) apps only for now: no client secret, no redirect URIs.

/// <summary>A registered partner app. Id is the public client_id (<c>lc_</c> + 24 hex).</summary>
public class PartnerClient
{
    [Key]
    [MaxLength(40)]
    public string Id { get; set; } = string.Empty;

    /// <summary>Shown to the Admin on the Allow screen. No rename button: fix with a DB update.</summary>
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Space-separated scopes the app may ask for.</summary>
    [MaxLength(255)]
    public string Scopes { get; set; } = string.Empty;

    [MaxLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? DisabledAt { get; set; }
}

/// <summary>One company's permission for one partner app. Disconnect = <see cref="RevokedAt"/>.</summary>
public class PartnerConnection
{
    [Key]
    [MaxLength(40)]
    public string Id { get; set; } = string.Empty;

    public int CompanyId { get; set; }

    [MaxLength(40)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>The Admin who allowed it. The connection dies if they are deactivated or lose Admin.</summary>
    [MaxLength(450)]
    public string ApprovedByUserId { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Scopes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public DateTime? RevokedAt { get; set; }
}

/// <summary>An access/refresh pair. A refresh replaces the pair (the old row is revoked).</summary>
public class PartnerToken
{
    [Key]
    [MaxLength(40)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(40)]
    public string ConnectionId { get; set; } = string.Empty;

    [MaxLength(64)]
    public string AccessHash { get; set; } = string.Empty;

    public DateTime AccessExpiresAt { get; set; }

    [MaxLength(64)]
    public string RefreshHash { get; set; } = string.Empty;

    public DateTime RefreshExpiresAt { get; set; }

    [MaxLength(255)]
    public string Scopes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }
}

/// <summary>RFC 8628 device authorization. Status: pending → approved|denied → used.</summary>
public class PartnerDeviceCode
{
    [Key]
    [MaxLength(64)]
    public string DeviceHash { get; set; } = string.Empty;

    [MaxLength(64)]
    public string UserCodeHash { get; set; } = string.Empty;

    [MaxLength(40)]
    public string ClientId { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Scopes { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Status { get; set; } = "pending";

    /// <summary>Set on approve.</summary>
    public int? CompanyId { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    /// <summary>Seconds; +5 per too-fast poll, max 60.</summary>
    public int PollInterval { get; set; }

    public DateTime? LastPolledAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}

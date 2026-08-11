using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace LosLms.Models;

/// <summary>
/// A real login. Replaces the old <c>Officer</c> table, which was an id and a name with no
/// credentials attached — every officer foreign key in the system now points here instead.
/// </summary>
/// <remarks>
/// Roles are Identity roles, not a column: Staff, Admin, SuperAdmin.
///
/// <see cref="CompanyId"/> is nullable for exactly one reason — a SuperAdmin operates across every
/// company and so belongs to none. Any other user without a company is a data error, and the global
/// query filter treats them as seeing nothing rather than seeing everything.
/// </remarks>
public class ApplicationUser : IdentityUser
{
    /// <summary>Name shown in dropdowns and the top bar, e.g. "R. Kulkarni".</summary>
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Owning tenant. Null only for SuperAdmin.</summary>
    public int? CompanyId { get; set; }

    /// <summary>
    /// Cleared instead of deleting a user, so historical foreign keys (who verified this RCU, who
    /// waived this PDD) keep resolving to a real name.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Set on seeded accounts, which are created with a generated temporary password. While true the
    /// user is redirected to /account/change-password and cannot reach anything else.
    /// </summary>
    /// <remarks>
    /// ASP.NET Core Identity has no built-in "temporary credential" or "must change password"
    /// mechanism — this flag plus the redirect guard in MainLayout is the whole implementation.
    /// </remarks>
    public bool MustChangePassword { get; set; }

    public Company? Company { get; set; }
}

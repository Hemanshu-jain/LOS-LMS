using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// A company's branch office. Replaces the five hardcoded branch names that used to be duplicated
/// as a <c>string[]</c> in the Applications Dashboard, Loan &amp; Security and Reports (RCU).
/// </summary>
/// <remarks>
/// Company-scoped by global query filter, so a branch dropdown can only ever offer the signed-in
/// user's own branches.
///
/// <see cref="Application.Branch"/> and <see cref="RcuInitiation.Branch"/> still store the branch
/// as a name string rather than a foreign key. Converting those to real FKs is a follow-up: it
/// touches the dashboard filter, the summary rail and the RCU screen, none of which this pass is
/// meant to disturb.
/// </remarks>
public class Branch
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Cleared instead of deleting a branch. A closed branch disappears from the dropdowns on new
    /// work but must never vanish from the applications it already sourced.
    /// </summary>
    /// <remarks>
    /// Those historical applications are safe either way: <see cref="Application.Branch"/> and
    /// <see cref="RcuInitiation.Branch"/> store the branch as a name string rather than a foreign
    /// key, so a deactivated — or even deleted — branch still renders on the files that used it.
    /// That is the one upside of the denormalisation noted below.
    /// </remarks>
    public bool IsActive { get; set; } = true;

    public Company? Company { get; set; }
}

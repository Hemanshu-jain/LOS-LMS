using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// A credit officer who can verify or approve an RCU outcome.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — reconciled during the cross-screen review once all nine screens exist.
///
/// ponytail: three fixed rows would normally be a Dictionary in code rather than a table. This one
/// earns the table — <see cref="RcuOutcome.VerifiedByOfficerId"/> and
/// <see cref="RcuInitiation.OverrideApproverOfficerId"/> are real foreign keys into it, so the
/// database is what enforces that an approver actually exists.
///
/// Note the inconsistency this creates: <c>Applications.AssignedOfficer</c> still stores an officer
/// as a plain name string. Two representations of the same people — recorded in
/// OPEN-QUESTIONS-FOR-ARUN.md.
/// </remarks>
public class Officer
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// One step in a loan's payout: submitted, approved, rejected, withdrawn, released, payee checked. Insert-only, so the Post Sanction
/// page can show exactly who did what and when, and nothing can rewrite it afterwards.
/// </summary>
public class DisbursementEvent
{
    public long Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    public DateTime At { get; set; }

    [MaxLength(30)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(450)]
    public string? UserId { get; set; }

    /// <summary>The person's name at the time, so the history still reads correctly if the user is later renamed or removed.</summary>
    [MaxLength(200)]
    public string? UserName { get; set; }

    [MaxLength(500)]
    public string? Detail { get; set; }

    public Application? Application { get; set; }
}

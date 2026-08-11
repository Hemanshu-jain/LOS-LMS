using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// A tenant. Every <see cref="Application"/>, <see cref="Branch"/> and non-SuperAdmin
/// <see cref="ApplicationUser"/> belongs to exactly one, and EF global query filters make that
/// boundary structural rather than something each screen has to remember.
/// </summary>
/// <remarks>
/// Deliberately minimal. Settings, thresholds and the rest of Company Setup are a later phase —
/// guessing at those columns now would bake in shapes nobody has agreed to.
///
/// Not itself query-filtered: a company row is only ever reached by id from a claim, and the
/// SuperAdmin screens need to enumerate all of them.
/// </remarks>
public class Company
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

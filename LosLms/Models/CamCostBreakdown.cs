using System.ComponentModel.DataAnnotations;

namespace LosLms.Models;

/// <summary>
/// Cost breakdown behind the Credit Appraisal Memo. One row per application.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — see <see cref="Application"/>.
///
/// The Draft/Applied split is deliberate. Everything the CAM displays — on-road cost, LTV, the
/// sanction tiles, the PDF — reads the Applied columns only. Draft values are what the officer is
/// currently typing. They become Applied solely when Recalculate is pressed, which is also what
/// stamps <see cref="LastRecalculatedAt"/>. The effect is that nobody can download a CAM.pdf
/// representing figures that were never actually computed.
/// </remarks>
public class CamCostBreakdown
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string ApplicationId { get; set; } = string.Empty;

    // ---- Draft: what the officer is editing ----
    public decimal? DraftExShowroomCost { get; set; }
    public decimal? DraftBodyAccessories { get; set; }
    public decimal? DraftInsuranceRegistration { get; set; }
    public decimal? DraftMargin { get; set; }

    // ---- Applied: what every computed figure and the PDF read ----
    public decimal? AppliedExShowroomCost { get; set; }
    public decimal? AppliedBodyAccessories { get; set; }
    public decimal? AppliedInsuranceRegistration { get; set; }
    public decimal? AppliedMargin { get; set; }

    /// <summary>Null means the CAM has never been recalculated. This is what gates CAM.pdf.</summary>
    public DateTime? LastRecalculatedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Application? Application { get; set; }
}

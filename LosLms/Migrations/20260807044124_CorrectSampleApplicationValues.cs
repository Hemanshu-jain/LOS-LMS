using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <summary>
    /// Corrects two wrong figures on LN-2026-004871, the sample application carried through the
    /// whole build.
    /// </summary>
    /// <remarks>
    /// Both are ordinary runtime rows rather than <c>HasData</c> seeds — the processing fee is typed
    /// at Stage 2 and the CAM row is created at Stage 3 — so there is no seed definition to edit.
    /// A data migration rather than a one-off UPDATE so the fix travels with the schema to any
    /// environment, following the same pattern as MigrateRemainingDsaChannelValues.
    ///
    /// Both figures were invented before the wireframe's own reference data was found:
    ///   AppliedMargin  410000 -> 320000  (the down payment the reference actually shows)
    ///   ProcessingFee   18500 ->  27750  (1.5%, not the 1.0% that was assumed)
    ///
    /// Stage 3's CAM tab reads both figures live, so it follows automatically. **Stage 7's Charges
    /// tab does not** — its Processing fee row is a snapshot taken from
    /// <c>Applications.ProcessingFee</c> the first time the screen is opened, because a charge has
    /// to be its own editable, waivable row. Correcting the parent column therefore leaves an
    /// already-seeded charge row stale, so the third statement below fixes that copy too. Stage 8's
    /// net disbursement is computed from those charge rows and would otherwise inherit the old fee.
    ///
    /// Statements are separate <c>Sql</c> calls because Pomelo sends a migration's SQL as one
    /// command by default, and MySQL rejects multiple statements in a single command.
    /// The rows may legitimately not exist yet on a fresh database, in which case both are no-ops.
    /// </remarks>
    public partial class CorrectSampleApplicationValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Draft as well as Applied. The CAM keeps the officer's in-progress figures separate
            // from the applied ones, so correcting Applied alone leaves the editable field showing
            // 410000, the tab reading "unsaved changes", and the next Recalculate promoting the old
            // value straight back over this correction.
            migrationBuilder.Sql(
                "UPDATE CamCostBreakdown SET AppliedMargin = 320000, DraftMargin = 320000 " +
                "WHERE ApplicationId = 'LN-2026-004871';");

            // Applications is keyed on Id, not ApplicationId — it is the parent row.
            migrationBuilder.Sql(
                "UPDATE Applications SET ProcessingFee = 27750 WHERE Id = 'LN-2026-004871';");

            // The Stage 7 charge row already seeded from the old figure. GST is 18% of the amount,
            // the same placeholder rate the Approvals screen seeds with.
            migrationBuilder.Sql(
                "UPDATE Charges SET Amount = 27750, Gst = 4995 " +
                "WHERE ApplicationId = 'LN-2026-004871' AND Head = 'Processing fee';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restores the figures this migration exists to correct — reversible, but reverting
            // reintroduces the known-wrong values.
            migrationBuilder.Sql(
                "UPDATE CamCostBreakdown SET AppliedMargin = 410000, DraftMargin = 410000 " +
                "WHERE ApplicationId = 'LN-2026-004871';");

            migrationBuilder.Sql(
                "UPDATE Applications SET ProcessingFee = 18500 WHERE Id = 'LN-2026-004871';");

            migrationBuilder.Sql(
                "UPDATE Charges SET Amount = 18500, Gst = 3330 " +
                "WHERE ApplicationId = 'LN-2026-004871' AND Head = 'Processing fee';");
        }
    }
}

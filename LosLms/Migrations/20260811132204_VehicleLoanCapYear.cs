using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class VehicleLoanCapYear : Migration
    {
        /// <inheritdoc />
        // Order matters on MySQL, and EF scaffolded it the wrong way round. InnoDB satisfies
        // FK_VehicleLoanCaps_Companies_CompanyId with whatever index leads on CompanyId — which is
        // this composite one — and refuses to drop the last index a foreign key depends on. Creating
        // the replacement first (it also leads on CompanyId) means the constraint is never uncovered,
        // so no FK has to be dropped and re-added.
        //
        // Only ever reproduced against MySQL on a database built from migrations. The portable SQLite
        // build calls EnsureCreated and never runs a migration, so it cannot surface this.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "VehicleLoanCaps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Safe to make unique immediately: the index being replaced was already unique on
            // (CompanyId, Make, Model), so no two existing rows can collide once they all take Year 0.
            migrationBuilder.CreateIndex(
                name: "IX_VehicleLoanCaps_CompanyId_Make_Model_Year",
                table: "VehicleLoanCaps",
                columns: new[] { "CompanyId", "Make", "Model", "Year" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_VehicleLoanCaps_CompanyId_Make_Model",
                table: "VehicleLoanCaps");
        }

        /// <inheritdoc />
        // Same ordering rule in reverse, for the same reason.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_VehicleLoanCaps_CompanyId_Make_Model",
                table: "VehicleLoanCaps",
                columns: new[] { "CompanyId", "Make", "Model" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_VehicleLoanCaps_CompanyId_Make_Model_Year",
                table: "VehicleLoanCaps");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "VehicleLoanCaps");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class VehicleLoanCapYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleLoanCaps_CompanyId_Make_Model",
                table: "VehicleLoanCaps");

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "VehicleLoanCaps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLoanCaps_CompanyId_Make_Model_Year",
                table: "VehicleLoanCaps",
                columns: new[] { "CompanyId", "Make", "Model", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleLoanCaps_CompanyId_Make_Model_Year",
                table: "VehicleLoanCaps");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "VehicleLoanCaps");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLoanCaps_CompanyId_Make_Model",
                table: "VehicleLoanCaps",
                columns: new[] { "CompanyId", "Make", "Model" },
                unique: true);
        }
    }
}

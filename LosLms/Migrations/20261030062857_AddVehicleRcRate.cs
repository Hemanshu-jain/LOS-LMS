using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleRcRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ApiRate",
                columns: new[] { "Code", "IsActive", "Name", "Product", "UnitRate" },
                values: new object[] { "KYC_VEHICLE_RC", true, "Vehicle RC verification", "DigiKYC", 2.00m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ApiRate",
                keyColumn: "Code",
                keyValue: "KYC_VEHICLE_RC");
        }
    }
}

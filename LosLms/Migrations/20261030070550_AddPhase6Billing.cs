using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase6Billing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VideoKycSeconds",
                table: "Parties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Uan",
                table: "DigioSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UpiVpa",
                table: "DigioSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Detail",
                table: "ApiUsageLog",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "ApiRate",
                columns: new[] { "Code", "IsActive", "Name", "Product", "UnitRate" },
                values: new object[] { "VIDEO_KYC_INHOUSE", true, "Video KYC (in-house)", "Video KYC", 1.00m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ApiRate",
                keyColumn: "Code",
                keyValue: "VIDEO_KYC_INHOUSE");

            migrationBuilder.DropColumn(
                name: "VideoKycSeconds",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "Uan",
                table: "DigioSettings");

            migrationBuilder.DropColumn(
                name: "UpiVpa",
                table: "DigioSettings");

            migrationBuilder.DropColumn(
                name: "Detail",
                table: "ApiUsageLog");
        }
    }
}

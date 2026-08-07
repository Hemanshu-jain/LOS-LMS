using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class MigrateRemainingDsaChannelValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004802",
                column: "SourcingChannel",
                value: "DSA — Patil Motors");

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004831",
                column: "SourcingChannel",
                value: "DSA — Patil Motors");

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004859",
                column: "SourcingChannel",
                value: "DSA — Patil Motors");

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004871",
                column: "SourcingChannel",
                value: "DSA — Patil Motors");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004802",
                column: "SourcingChannel",
                value: "DSA");

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004831",
                column: "SourcingChannel",
                value: "DSA");

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004859",
                column: "SourcingChannel",
                value: "DSA");

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004871",
                column: "SourcingChannel",
                value: "DSA");
        }
    }
}

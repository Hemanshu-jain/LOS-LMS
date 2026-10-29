using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class AddTgPortalRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TgPortalScreenshotPath",
                table: "SecurityDetails",
                type: "varchar(400)",
                maxLength: 400,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "TgPortalVerified",
                table: "SecurityDetails",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TgPortalVerifiedAt",
                table: "SecurityDetails",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TgPortalVerifiedBy",
                table: "SecurityDetails",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TgPortalVerifiedByUserId",
                table: "SecurityDetails",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TgPortalScreenshotPath",
                table: "SecurityDetails");

            migrationBuilder.DropColumn(
                name: "TgPortalVerified",
                table: "SecurityDetails");

            migrationBuilder.DropColumn(
                name: "TgPortalVerifiedAt",
                table: "SecurityDetails");

            migrationBuilder.DropColumn(
                name: "TgPortalVerifiedBy",
                table: "SecurityDetails");

            migrationBuilder.DropColumn(
                name: "TgPortalVerifiedByUserId",
                table: "SecurityDetails");
        }
    }
}

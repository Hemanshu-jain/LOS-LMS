using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyFirmProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Constitution",
                table: "Companies",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Gstin",
                table: "Companies",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateOnly>(
                name: "IncorpDate",
                table: "Companies",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Vintage",
                table: "Companies",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Constitution", "Gstin", "IncorpDate", "Vintage" },
                values: new object[] { null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Constitution",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Gstin",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "IncorpDate",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Vintage",
                table: "Companies");
        }
    }
}

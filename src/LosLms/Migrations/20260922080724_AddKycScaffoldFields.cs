using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class AddKycScaffoldFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "AadhaarGeneratedOn",
                table: "Parties",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AddressChangedSinceAadhaar",
                table: "Parties",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AddressProofPath",
                table: "Parties",
                type: "varchar(400)",
                maxLength: 400,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "SignedConfirmed",
                table: "Documents",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AadhaarGeneratedOn",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "AddressChangedSinceAadhaar",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "AddressProofPath",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "SignedConfirmed",
                table: "Documents");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class IfscAndNachMandateFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "SecurityNachMandate",
                type: "varchar(18)",
                maxLength: 18,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BankBranch",
                table: "SecurityNachMandate",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "SecurityNachMandate",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "ConfirmationAccepted",
                table: "SecurityNachMandate",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DigitalMode",
                table: "SecurityNachMandate",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Ifsc",
                table: "SecurityNachMandate",
                type: "varchar(11)",
                maxLength: 11,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MandateType",
                table: "SecurityNachMandate",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "NameMatchCheckedAt",
                table: "SecurityNachMandate",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameMatchStatus",
                table: "SecurityNachMandate",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotRun")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "EnachMandate",
                type: "varchar(18)",
                maxLength: 18,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BankBranch",
                table: "EnachMandate",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "EnachMandate",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "ConfirmationAccepted",
                table: "EnachMandate",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DigitalMode",
                table: "EnachMandate",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Ifsc",
                table: "EnachMandate",
                type: "varchar(11)",
                maxLength: 11,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MandateType",
                table: "EnachMandate",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "NameMatchCheckedAt",
                table: "EnachMandate",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameMatchStatus",
                table: "EnachMandate",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotRun")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BankAddress",
                table: "BankDetails",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BankBranch",
                table: "BankDetails",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "BankBranch",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "ConfirmationAccepted",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "DigitalMode",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "Ifsc",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "MandateType",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "NameMatchCheckedAt",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "NameMatchStatus",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "BankBranch",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "ConfirmationAccepted",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "DigitalMode",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "Ifsc",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "MandateType",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "NameMatchCheckedAt",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "NameMatchStatus",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "BankAddress",
                table: "BankDetails");

            migrationBuilder.DropColumn(
                name: "BankBranch",
                table: "BankDetails");
        }
    }
}

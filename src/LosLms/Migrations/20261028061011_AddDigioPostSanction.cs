using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class AddDigioPostSanction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthMode",
                table: "SecurityNachMandate",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BankState",
                table: "SecurityNachMandate",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "CollectionAmount",
                table: "SecurityNachMandate",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationBankId",
                table: "SecurityNachMandate",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DigioMandateId",
                table: "SecurityNachMandate",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DigioRequestId",
                table: "SecurityNachMandate",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DigioState",
                table: "SecurityNachMandate",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumAmount",
                table: "SecurityNachMandate",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "SecurityNachMandate",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "SecurityNachMandate",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SignedFormPath",
                table: "SecurityNachMandate",
                type: "varchar(400)",
                maxLength: 400,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AuthMode",
                table: "EnachMandate",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BankState",
                table: "EnachMandate",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "CollectionAmount",
                table: "EnachMandate",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationBankId",
                table: "EnachMandate",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DigioMandateId",
                table: "EnachMandate",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DigioRequestId",
                table: "EnachMandate",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DigioState",
                table: "EnachMandate",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumAmount",
                table: "EnachMandate",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegisteredAt",
                table: "EnachMandate",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "EnachMandate",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SignedFormPath",
                table: "EnachMandate",
                type: "varchar(400)",
                maxLength: 400,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AgreementFingerprint",
                table: "Disbursement",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AgreementHash",
                table: "Disbursement",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EsignRequestId",
                table: "Disbursement",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EnachConfigIdProduction",
                table: "DigioSettings",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EnachConfigIdSandbox",
                table: "DigioSettings",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EstampTag",
                table: "DigioSettings",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LenderSignaturePath",
                table: "DigioSettings",
                type: "varchar(400)",
                maxLength: 400,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NachCategory",
                table: "DigioSettings",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "L001")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "NachFinalBufferMonths",
                table: "DigioSettings",
                type: "int",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.AddColumn<string>(
                name: "NachFrequency",
                table: "DigioSettings",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Adhoc")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "NachMaxMultiplier",
                table: "DigioSettings",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 2m);

            migrationBuilder.CreateTable(
                name: "EsignSigner",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ApplicationId = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocumentId = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PartyId = table.Column<int>(type: "int", nullable: true),
                    PartyType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Identifier = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SignOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FailureReason = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EsignSigner", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EsignSigner_CompanyId_ApplicationId_DocumentId",
                table: "EsignSigner",
                columns: new[] { "CompanyId", "ApplicationId", "DocumentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EsignSigner");

            migrationBuilder.DropColumn(
                name: "AuthMode",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "BankState",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "CollectionAmount",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "DestinationBankId",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "DigioMandateId",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "DigioRequestId",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "DigioState",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "MaximumAmount",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "SignedFormPath",
                table: "SecurityNachMandate");

            migrationBuilder.DropColumn(
                name: "AuthMode",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "BankState",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "CollectionAmount",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "DestinationBankId",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "DigioMandateId",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "DigioRequestId",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "DigioState",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "MaximumAmount",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "RegisteredAt",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "SignedFormPath",
                table: "EnachMandate");

            migrationBuilder.DropColumn(
                name: "AgreementFingerprint",
                table: "Disbursement");

            migrationBuilder.DropColumn(
                name: "AgreementHash",
                table: "Disbursement");

            migrationBuilder.DropColumn(
                name: "EsignRequestId",
                table: "Disbursement");

            migrationBuilder.DropColumn(
                name: "EnachConfigIdProduction",
                table: "DigioSettings");

            migrationBuilder.DropColumn(
                name: "EnachConfigIdSandbox",
                table: "DigioSettings");

            migrationBuilder.DropColumn(
                name: "EstampTag",
                table: "DigioSettings");

            migrationBuilder.DropColumn(
                name: "LenderSignaturePath",
                table: "DigioSettings");

            migrationBuilder.DropColumn(
                name: "NachCategory",
                table: "DigioSettings");

            migrationBuilder.DropColumn(
                name: "NachFinalBufferMonths",
                table: "DigioSettings");

            migrationBuilder.DropColumn(
                name: "NachFrequency",
                table: "DigioSettings");

            migrationBuilder.DropColumn(
                name: "NachMaxMultiplier",
                table: "DigioSettings");
        }
    }
}

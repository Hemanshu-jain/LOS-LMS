using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class DisbursementPayoutControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClearedByUserId",
                table: "PostSanctionChecklists",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalNote",
                table: "Disbursement",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedAmount",
                table: "Disbursement",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Disbursement",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserId",
                table: "Disbursement",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ApprovedFingerprint",
                table: "Disbursement",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryBank",
                table: "Disbursement",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryIfsc",
                table: "Disbursement",
                type: "varchar(11)",
                maxLength: 11,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PayoutStatus",
                table: "Disbursement",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Draft")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleasedAt",
                table: "Disbursement",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReleasedByUserId",
                table: "Disbursement",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Disbursement",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedByUserId",
                table: "Disbursement",
                type: "varchar(450)",
                maxLength: 450,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DisbursementEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationId = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    At = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Action = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Detail = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisbursementEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisbursementEvents_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Loans released before this control existed: their payout is complete, so they must not reopen as "Draft".
            migrationBuilder.Sql(
                "UPDATE Disbursement SET PayoutStatus = 'Released' WHERE ApplicationId IN (SELECT Id FROM Applications WHERE Disbursed = 1);");

            migrationBuilder.CreateIndex(
                name: "IX_DisbursementEvents_ApplicationId",
                table: "DisbursementEvents",
                column: "ApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisbursementEvents");

            migrationBuilder.DropColumn(
                name: "ClearedByUserId",
                table: "PostSanctionChecklists");

            migrationBuilder.DropColumn(
                name: "ApprovalNote",
                table: "Disbursement");

            migrationBuilder.DropColumn(
                name: "ApprovedAmount",
                table: "Disbursement");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Disbursement");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "Disbursement");

            migrationBuilder.DropColumn(
                name: "ApprovedFingerprint",
                table: "Disbursement");

            migrationBuilder.DropColumn(
                name: "BeneficiaryBank",
                table: "Disbursement");

            migrationBuilder.DropColumn(
                name: "BeneficiaryIfsc",
                table: "Disbursement");

            migrationBuilder.DropColumn(
                name: "PayoutStatus",
                table: "Disbursement");

            migrationBuilder.DropColumn(
                name: "ReleasedAt",
                table: "Disbursement");

            migrationBuilder.DropColumn(
                name: "ReleasedByUserId",
                table: "Disbursement");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Disbursement");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "Disbursement");
        }
    }
}

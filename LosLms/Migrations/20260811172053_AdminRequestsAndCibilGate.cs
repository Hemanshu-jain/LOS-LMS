using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class AdminRequestsAndCibilGate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CibilCheckedAt",
                table: "Parties",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CibilScore",
                table: "Parties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CibilStatus",
                table: "Parties",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                // EF defaults a new non-null string column to "". Existing parties have genuinely
                // not been checked, and that is what the column should say.
                defaultValue: "NotChecked")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CibilGateStatus",
                table: "Applications",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                // Same reason. An application nobody has run a bureau check on is Blocked, not "".
                defaultValue: "Blocked")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AdminRequest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationId = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestedByUserId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RequestReason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubjectKey = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewedByUserId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReviewNote = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminRequest_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdminRequest_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdminRequest_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AdminRequest_ApplicationId_RequestType_Status",
                table: "AdminRequest",
                columns: new[] { "ApplicationId", "RequestType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminRequest_RequestedByUserId",
                table: "AdminRequest",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminRequest_ReviewedByUserId",
                table: "AdminRequest",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminRequest_Status",
                table: "AdminRequest",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminRequest");

            migrationBuilder.DropColumn(
                name: "CibilCheckedAt",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "CibilScore",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "CibilStatus",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "CibilGateStatus",
                table: "Applications");
        }
    }
}

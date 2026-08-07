using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class AssignedOfficerAndRejection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedOfficerId",
                table: "Applications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RejectionLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationId = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StageAtRejection = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RejectedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RejectionLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RejectionLog_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004279",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004282",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004286",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004288",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004290",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004295",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004301",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004306",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004309",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004316",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004319",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004323",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004331",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004336",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004343",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004345",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004351",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004355",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004359",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004364",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004366",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004369",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004371",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004374",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004380",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004382",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004384",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004392",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004397",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004399",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004405",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004412",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004416",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004420",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004423",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004428",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004431",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004434",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004436",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004439",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004441",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004448",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004452",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004456",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004459",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004461",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004464",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004468",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004470",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004478",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004481",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004485",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004487",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004490",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004492",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004500",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004502",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004505",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004513",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004515",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004517",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004522",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004525",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004532",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004540",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004547",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004551",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004555",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004560",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004564",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004571",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004576",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004580",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004582",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004586",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004591",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004598",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004601",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004605",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004610",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004613",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004620",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004624",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004627",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004633",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004638",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004646",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004653",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004656",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004663",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004665",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004670",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004672",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004675",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004677",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004680",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004686",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004693",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004696",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004700",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004707",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004715",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004723",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004725",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004729",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004735",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004743",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004747",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004750",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004754",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004761",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004767",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004770",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004773",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004776",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004782",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004785",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004788",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004790",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004795",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004802",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004811",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004820",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004831",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004844",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004859",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004868",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004871",
                column: "AssignedOfficerId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_AssignedOfficerId",
                table: "Applications",
                column: "AssignedOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_RejectionLog_ApplicationId",
                table: "RejectionLog",
                column: "ApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Officers_AssignedOfficerId",
                table: "Applications",
                column: "AssignedOfficerId",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Backfill the new FK from the existing officer name string, so all pre-existing
            // applications keep their assignment and the dashboard filter + Summary Rail work on
            // real data from the first load. Must run AFTER the 128 seed UpdateData calls above,
            // which set AssignedOfficerId to NULL — this restores the real value on top of them.
            // The three seeded names all match Officers.Name exactly; any that did not simply stay
            // NULL. Matches the MigrateRemainingDsaChannelValues data-migration precedent.
            migrationBuilder.Sql(
                "UPDATE Applications a JOIN Officers o ON o.Name = a.AssignedOfficer " +
                "SET a.AssignedOfficerId = o.Id;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Officers_AssignedOfficerId",
                table: "Applications");

            migrationBuilder.DropTable(
                name: "RejectionLog");

            migrationBuilder.DropIndex(
                name: "IX_Applications_AssignedOfficerId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AssignedOfficerId",
                table: "Applications");
        }
    }
}

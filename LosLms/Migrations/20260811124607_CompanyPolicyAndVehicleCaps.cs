using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class CompanyPolicyAndVehicleCaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Companies",
                type: "varchar(400)",
                maxLength: 400,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "AddressValidityDays",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CibilMaxScore",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CibilMinScore",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Companies",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Companies",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "FoirCapPct",
                table: "Companies",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FoirRiskCautionPct",
                table: "Companies",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FoirRiskDangerPct",
                table: "Companies",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GstPct",
                table: "Companies",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LtvCapPct",
                table: "Companies",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LtvRiskCautionPct",
                table: "Companies",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LtvRiskDangerPct",
                table: "Companies",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MinimumReferences",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "NoteStaleTolerancePct",
                table: "Companies",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SlaOverdueDays",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Companies",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Branches",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "VehicleLoanCaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Make = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Model = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaxLoanAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleLoanCaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleLoanCaps_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 4,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 5,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Address", "AddressValidityDays", "CibilMaxScore", "CibilMinScore", "ContactEmail", "ContactPhone", "FoirCapPct", "FoirRiskCautionPct", "FoirRiskDangerPct", "GstPct", "LtvCapPct", "LtvRiskCautionPct", "LtvRiskDangerPct", "MinimumReferences", "NoteStaleTolerancePct", "SlaOverdueDays", "UpdatedAt" },
                values: new object[] { null, 90, 900, 300, null, null, 50m, 40m, 60m, 18m, 85m, 75m, 90m, 2, 0.5m, 5, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLoanCaps_CompanyId_Make_Model",
                table: "VehicleLoanCaps",
                columns: new[] { "CompanyId", "Make", "Model" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleLoanCaps");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "AddressValidityDays",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CibilMaxScore",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CibilMinScore",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "FoirCapPct",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "FoirRiskCautionPct",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "FoirRiskDangerPct",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "GstPct",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LtvCapPct",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LtvRiskCautionPct",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LtvRiskDangerPct",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "MinimumReferences",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "NoteStaleTolerancePct",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SlaOverdueDays",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Branches");
        }
    }
}

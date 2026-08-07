using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class LoanSecuritySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tenure and Roi were stored as display strings ("48 mo", "13.25%"). EF scaffolded a
            // plain AlterColumn to int/decimal here, which MySQL rejects under strict mode as soon
            // as it meets one of those values. Each column is instead converted through a temporary
            // column that extracts the numeric portion first, so existing rows survive.
            //
            // '[0-9.]+' is a character class, so the dot is literal and needs no backslash — which
            // avoids escaping it through both the C# string and MySQL's own string parser.
            // Every statement is its own Sql() call rather than one semicolon-separated batch.
            migrationBuilder.Sql("ALTER TABLE `Applications` ADD COLUMN `TenureConverted` int NULL;");
            migrationBuilder.Sql(
                "UPDATE `Applications` SET `TenureConverted` = CAST(REGEXP_SUBSTR(`Tenure`, '[0-9]+') AS SIGNED) " +
                "WHERE `Tenure` IS NOT NULL AND REGEXP_SUBSTR(`Tenure`, '[0-9]+') IS NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE `Applications` DROP COLUMN `Tenure`;");
            migrationBuilder.Sql("ALTER TABLE `Applications` CHANGE `TenureConverted` `Tenure` int NULL;");

            migrationBuilder.Sql("ALTER TABLE `Applications` ADD COLUMN `RoiConverted` decimal(5,2) NULL;");
            migrationBuilder.Sql(
                "UPDATE `Applications` SET `RoiConverted` = CAST(REGEXP_SUBSTR(`Roi`, '[0-9.]+') AS DECIMAL(5,2)) " +
                "WHERE `Roi` IS NOT NULL AND REGEXP_SUBSTR(`Roi`, '[0-9.]+') IS NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE `Applications` DROP COLUMN `Roi`;");
            migrationBuilder.Sql("ALTER TABLE `Applications` CHANGE `RoiConverted` `Roi` decimal(5,2) NULL;");

            migrationBuilder.AddColumn<decimal>(
                name: "AdvanceEmi",
                table: "Applications",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DisbursalDate",
                table: "Applications",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProcessingFee",
                table: "Applications",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepaymentMode",
                table: "Applications",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "References",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationId = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Relationship = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mobile = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KnownSince = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_References", x => x.Id);
                    table.ForeignKey(
                        name: "FK_References_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SecurityDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationId = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssetType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MakeModel = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MfgYear = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RegNo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChassisNo = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EngineNo = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InvoiceNo = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InvoiceValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    InsuranceProvider = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PolicyNo = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PolicyExpiry = table.Column<DateOnly>(type: "date", nullable: true),
                    PropertyType = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PropertyAddress = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Area = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OwnershipType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SaleDeedNo = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValuationRefNo = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EncumbranceRef = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssessedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    InvoiceFilePath = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InsuranceFilePath = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityDetails_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Viability",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationId = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IncomeFreight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    IncomeSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    IncomeOther = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    ExpenseHousehold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    ExpenseFuelDriver = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    ExpenseExistingEmi = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Viability", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Viability_Applications_ApplicationId",
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
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004282",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.25m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004286",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.00m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004288",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004290",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004295",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.00m, "DSA — Patil Motors", 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004301",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004306",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004309",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.25m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004316",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.75m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004319",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, "DSA — Patil Motors", 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004323",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.25m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004331",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.00m, "DSA — Patil Motors", 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004336",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 14.25m, "DSA — Patil Motors", 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004343",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.75m, "DSA — Patil Motors", 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004345",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.25m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004351",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, "DSA — Patil Motors", 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004355",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004359",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.25m, "DSA — Patil Motors", 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004364",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, "DSA — Patil Motors", 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004366",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 12.75m, "DSA — Patil Motors", 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004369",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004371",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004374",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 12.50m, "DSA — Patil Motors", 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004380",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004382",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004384",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.75m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004392",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 12.50m, "DSA — Patil Motors", 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004397",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.75m, "DSA — Patil Motors", 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004399",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.25m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004405",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, "DSA — Patil Motors", 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004412",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.00m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004416",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004420",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.75m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004423",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004428",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.50m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004431",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.25m, "DSA — Patil Motors", 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004434",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, "DSA — Patil Motors", 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004436",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004439",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.00m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004441",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004448",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.00m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004452",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, "DSA — Patil Motors", 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004456",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.75m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004459",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004461",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004464",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.50m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004468",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004470",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004478",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.75m, "DSA — Patil Motors", 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004481",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, "DSA — Patil Motors", 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004485",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.25m, "DSA — Patil Motors", 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004487",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.75m, "DSA — Patil Motors", 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004490",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004492",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.75m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004500",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.50m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004502",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.00m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004505",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.75m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004513",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004515",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, "DSA — Patil Motors", 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004517",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, "DSA — Patil Motors", 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004522",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, "DSA — Patil Motors", 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004525",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.25m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004532",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.75m, "DSA — Patil Motors", 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004540",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004547",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.00m, "DSA — Patil Motors", 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004551",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004555",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 12.75m, "DSA — Patil Motors", 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004560",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, "DSA — Patil Motors", 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004564",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.75m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004571",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004576",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004580",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004582",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.75m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004586",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.75m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004591",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.75m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004598",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, "DSA — Patil Motors", 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004601",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.00m, "DSA — Patil Motors", 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004605",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004610",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.00m, "DSA — Patil Motors", 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004613",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.75m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004620",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.25m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004624",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.25m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004627",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.75m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004633",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.25m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004638",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.25m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004646",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.00m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004653",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.50m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004656",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.25m, "DSA — Patil Motors", 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004663",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.75m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004665",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.25m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004670",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004672",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 12.75m, "DSA — Patil Motors", 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004675",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004677",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004680",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.50m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004686",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004693",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, "DSA — Patil Motors", 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004696",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.25m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004700",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.00m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004707",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.50m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004715",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004723",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.25m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004725",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.50m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004729",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004735",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.75m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004743",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.50m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004747",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.50m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004750",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 14.25m, "DSA — Patil Motors", 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004754",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.00m, "DSA — Patil Motors", 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004761",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.50m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004767",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004770",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 12.50m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004773",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.25m, "DSA — Patil Motors", 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004776",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004782",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 13.25m, "DSA — Patil Motors", 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004785",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.00m, 48 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004788",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { null, null, null, null, 12.50m, "DSA — Patil Motors", 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004790",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.00m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004795",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.25m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004802",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.75m, 60 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004811",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.75m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004820",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.75m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004831",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.25m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004844",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.00m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004859",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, 72 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004868",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 14.50m, 36 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004871",
                columns: new[] { "AdvanceEmi", "DisbursalDate", "ProcessingFee", "RepaymentMode", "Roi", "Tenure" },
                values: new object[] { null, null, null, null, 13.25m, 48 });

            migrationBuilder.CreateIndex(
                name: "IX_References_ApplicationId",
                table: "References",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityDetails_ApplicationId",
                table: "SecurityDetails",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Viability_ApplicationId",
                table: "Viability",
                column: "ApplicationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "References");

            migrationBuilder.DropTable(
                name: "SecurityDetails");

            migrationBuilder.DropTable(
                name: "Viability");

            migrationBuilder.DropColumn(
                name: "AdvanceEmi",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "DisbursalDate",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ProcessingFee",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "RepaymentMode",
                table: "Applications");

            migrationBuilder.AlterColumn<string>(
                name: "Tenure",
                table: "Applications",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Roi",
                table: "Applications",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004279",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.50%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004282",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.25%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004286",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.00%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004288",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.50%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004290",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.00%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004295",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.00%", "DSA", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004301",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.50%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004306",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.50%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004309",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.25%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004316",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.75%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004319",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "14.50%", "DSA", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004323",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.25%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004331",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.00%", "DSA", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004336",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "14.25%", "DSA", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004343",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.75%", "DSA", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004345",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.25%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004351",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.50%", "DSA", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004355",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.50%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004359",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.25%", "DSA", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004364",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "14.50%", "DSA", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004366",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "12.75%", "DSA", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004369",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.50%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004371",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.00%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004374",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "12.50%", "DSA", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004380",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.00%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004382",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.50%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004384",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.75%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004392",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "12.50%", "DSA", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004397",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.75%", "DSA", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004399",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.25%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004405",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.50%", "DSA", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004412",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.00%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004416",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.00%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004420",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.75%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004423",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.50%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004428",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.50%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004431",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.25%", "DSA", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004434",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "14.00%", "DSA", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004436",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.50%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004439",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.00%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004441",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.50%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004448",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.00%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004452",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "14.00%", "DSA", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004456",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.75%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004459",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.50%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004461",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.00%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004464",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.50%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004468",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.50%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004470",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.00%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004478",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.75%", "DSA", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004481",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "14.50%", "DSA", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004485",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.25%", "DSA", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004487",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.75%", "DSA", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004490",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.50%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004492",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.75%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004500",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.50%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004502",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.00%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004505",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.75%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004513",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.50%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004515",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "14.00%", "DSA", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004517",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "14.50%", "DSA", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004522",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.50%", "DSA", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004525",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.25%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004532",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.75%", "DSA", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004540",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.50%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004547",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.00%", "DSA", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004551",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.00%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004555",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "12.75%", "DSA", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004560",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "14.00%", "DSA", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004564",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.75%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004571",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.50%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004576",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.50%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004580",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.50%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004582",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.75%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004586",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.75%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004591",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.75%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004598",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "14.50%", "DSA", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004601",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.00%", "DSA", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004605",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.00%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004610",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.00%", "DSA", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004613",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.75%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004620",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.25%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004624",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.25%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004627",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.75%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004633",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.25%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004638",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.25%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004646",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.00%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004653",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.50%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004656",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.25%", "DSA", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004663",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.75%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004665",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.25%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004670",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.50%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004672",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "12.75%", "DSA", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004675",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.50%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004677",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.00%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004680",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.50%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004686",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.00%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004693",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.50%", "DSA", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004696",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.25%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004700",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.00%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004707",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.50%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004715",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.50%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004723",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.25%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004725",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.50%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004729",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.50%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004735",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.75%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004743",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.50%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004747",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.50%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004750",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "14.25%", "DSA", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004754",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.00%", "DSA", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004761",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.50%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004767",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.00%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004770",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "12.50%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004773",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.25%", "DSA", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004776",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.00%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004782",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "13.25%", "DSA", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004785",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.00%", "48 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004788",
                columns: new[] { "Roi", "SourcingChannel", "Tenure" },
                values: new object[] { "12.50%", "DSA", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004790",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.00%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004795",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.25%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004802",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.75%", "60 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004811",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.75%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004820",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.75%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004831",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.25%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004844",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.00%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004859",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.50%", "72 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004868",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "14.50%", "36 mo" });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004871",
                columns: new[] { "Roi", "Tenure" },
                values: new object[] { "13.25%", "48 mo" });
        }
    }
}

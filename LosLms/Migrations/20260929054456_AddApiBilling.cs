using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class AddApiBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiInvoice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Gst = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PaymentNote = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiInvoice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiInvoice_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ApiRate",
                columns: table => new
                {
                    Code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Product = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnitRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiRate", x => x.Code);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ApiUsageLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    ApiCode = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApplicationId = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderRef = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSuccess = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UnitRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiUsageLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiUsageLog_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "ApiRate",
                columns: new[] { "Code", "IsActive", "Name", "Product", "UnitRate" },
                values: new object[,]
                {
                    { "BANK_PENNY_CHEQUE", true, "Cheque + penny drop", "Bank verification", 3.50m },
                    { "BANK_PENNY_DROP", true, "Penny drop (independent)", "Bank verification", 2.50m },
                    { "BANK_PENNY_REVERSE", true, "Reverse penny drop", "Bank verification", 2.00m },
                    { "BANK_PENNYLESS", true, "Pennyless check", "Bank verification", 2.00m },
                    { "BANK_STMT_ANALYZER", true, "Bank statement analyzer (per statement)", "Bank verification", 13.00m },
                    { "ESIGN_AADHAAR", true, "Aadhaar e-Sign (per credit)", "DigiSign", 10.60m },
                    { "KRA_CHECK", true, "KRA check", "DigiKYC", 1.00m },
                    { "KRA_DOWNLOAD", true, "KRA document download", "DigiKYC", 1.00m },
                    { "KRA_FETCH", true, "KRA detail fetch", "DigiKYC", 1.00m },
                    { "KRA_MODIFY", true, "KRA modify", "DigiKYC", 1.00m },
                    { "KRA_SUBMIT", true, "KRA submission", "DigiKYC", 1.00m },
                    { "KYC_AADHAAR_MASK", true, "Aadhaar masking", "DigiKYC", 1.00m },
                    { "KYC_AADHAAR_XML", true, "Aadhaar Offline XML KYC", "DigiKYC", 2.00m },
                    { "KYC_BUSINESS", true, "Business KYC (GST, PAN, DIN, CIN…)", "DigiKYC", 3.00m },
                    { "KYC_DIGILOCKER", true, "DigiLocker document fetch", "DigiKYC", 2.00m },
                    { "KYC_FACE_MATCH", true, "Face match", "DigiKYC", 1.00m },
                    { "KYC_FUZZY_MATCH", true, "Name/address fuzzy match", "DigiKYC", 1.00m },
                    { "KYC_GEOLOCATION", true, "Geolocation add-on", "DigiKYC", 0.50m },
                    { "KYC_ID_OCR", true, "ID card OCR (Aadhaar/PAN/Voter/Passport/RC)", "DigiKYC", 2.00m },
                    { "KYC_ID_VERIFY", true, "ID verification (PAN, Voter ID…)", "DigiKYC", 1.50m },
                    { "KYC_SELFIE", true, "Selfie with liveness + geotag", "DigiKYC", 2.50m },
                    { "KYC_UAN", true, "UAN verification", "DigiKYC", 15.00m },
                    { "KYC_UPI_VPA", true, "UPI VPA verification", "DigiKYC", 1.00m },
                    { "KYC_VIDEO_1WAY", true, "1-way Video KYC", "DigiKYC", 6.00m },
                    { "MANDATE_API", true, "e-Mandate (netbanking/debit card/Aadhaar)", "DigiCollect", 4.50m },
                    { "MANDATE_DEBIT", true, "ACH debit presentation", "DigiCollect", 2.00m },
                    { "MANDATE_ESIGN", true, "eSign eNACH mandate", "DigiCollect", 12.00m },
                    { "MANDATE_PHYSICAL", true, "Physical / scan NACH mandate", "DigiCollect", 5.00m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiInvoice_CompanyId_PeriodStart",
                table: "ApiInvoice",
                columns: new[] { "CompanyId", "PeriodStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageLog_CompanyId_CreatedAt",
                table: "ApiUsageLog",
                columns: new[] { "CompanyId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiInvoice");

            migrationBuilder.DropTable(
                name: "ApiRate");

            migrationBuilder.DropTable(
                name: "ApiUsageLog");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class CompanyLegalIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorisedSignatoryDesignation",
                table: "Companies",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AuthorisedSignatoryName",
                table: "Companies",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Cin",
                table: "Companies",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GrievanceOfficerEmail",
                table: "Companies",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GrievanceOfficerName",
                table: "Companies",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GrievanceOfficerPhone",
                table: "Companies",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RbiNbfcRegNo",
                table: "Companies",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RegisteredCity",
                table: "Companies",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Companies",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AuthorisedSignatoryDesignation", "AuthorisedSignatoryName", "Cin", "GrievanceOfficerEmail", "GrievanceOfficerName", "GrievanceOfficerPhone", "RbiNbfcRegNo", "RegisteredCity", "Website" },
                values: new object[] { null, null, null, null, null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorisedSignatoryDesignation",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "AuthorisedSignatoryName",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Cin",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "GrievanceOfficerEmail",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "GrievanceOfficerName",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "GrievanceOfficerPhone",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "RbiNbfcRegNo",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "RegisteredCity",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Companies");
        }
    }
}

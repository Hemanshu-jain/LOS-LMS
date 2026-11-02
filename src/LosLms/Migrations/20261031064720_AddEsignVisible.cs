using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class AddEsignVisible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SignedAt",
                table: "EsignSigner",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "ApiRate",
                columns: new[] { "Code", "IsActive", "Name", "Product", "UnitRate" },
                values: new object[] { "ESIGN_COORDINATES", true, "DigiSign: custom signature coordinates (per signatory)", "DigiSign", 1.50m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ApiRate",
                keyColumn: "Code",
                keyValue: "ESIGN_COORDINATES");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                table: "EsignSigner");
        }
    }
}

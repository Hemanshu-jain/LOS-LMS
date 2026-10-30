using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class AddDigioBillingRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ApiRate",
                columns: new[] { "Code", "IsActive", "Name", "Product", "UnitRate" },
                values: new object[,]
                {
                    { "ESIGN_DOWNLOAD", true, "DigiSign: download signed copy (sign-only API)", "DigiSign", 0.75m },
                    { "ESIGN_SEND", true, "DigiSign: send document (sign-only API)", "DigiSign", 0.75m },
                    { "MANDATE_CANCEL", true, "Mandate cancellation", "DigiCollect", 2.00m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ApiRate",
                keyColumn: "Code",
                keyValue: "ESIGN_DOWNLOAD");

            migrationBuilder.DeleteData(
                table: "ApiRate",
                keyColumn: "Code",
                keyValue: "ESIGN_SEND");

            migrationBuilder.DeleteData(
                table: "ApiRate",
                keyColumn: "Code",
                keyValue: "MANDATE_CANCEL");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class AddLivePhotoVideoKyc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LivePhotoPath",
                table: "Parties",
                type: "varchar(400)",
                maxLength: 400,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VideoKycPath",
                table: "Parties",
                type: "varchar(400)",
                maxLength: 400,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LivePhotoPath",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "VideoKycPath",
                table: "Parties");
        }
    }
}

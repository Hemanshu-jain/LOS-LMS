using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationEmiDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmiDayOfMonth",
                table: "Applications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FirstEmiDate",
                table: "Applications",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmiDayOfMonth",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "FirstEmiDate",
                table: "Applications");
        }
    }
}

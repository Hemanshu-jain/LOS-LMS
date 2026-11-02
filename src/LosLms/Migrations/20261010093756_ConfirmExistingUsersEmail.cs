using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class ConfirmExistingUsersEmail : Migration
    {
        public const string ConfirmEveryoneSql = "UPDATE AspNetUsers SET EmailConfirmed = 1 WHERE EmailConfirmed = 0";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Email confirmation is new, for NEW self-sign-ups only. Everyone who exists today (company owners, staff an Admin
            // created, the seeded accounts) must stay able to sign in, so they are all marked confirmed once, here.
            migrationBuilder.Sql(ConfirmEveryoneSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

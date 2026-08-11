using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class IdentityCompaniesAndBranches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Officers_AssignedOfficerId",
                table: "Applications");

            migrationBuilder.DropForeignKey(
                name: "FK_Pdd_Officers_WaivedByOfficerId",
                table: "Pdd");

            migrationBuilder.DropForeignKey(
                name: "FK_RcuInitiation_Officers_OverrideApproverOfficerId",
                table: "RcuInitiation");

            migrationBuilder.DropForeignKey(
                name: "FK_RcuOutcomes_Officers_VerifiedByOfficerId",
                table: "RcuOutcomes");

            migrationBuilder.DropTable(
                name: "Officers");

            migrationBuilder.DropColumn(
                name: "ApproverName",
                table: "ApprovalDecision");

            migrationBuilder.DropColumn(
                name: "RecommenderName",
                table: "ApprovalDecision");

            migrationBuilder.AlterColumn<string>(
                name: "VerifiedByOfficerId",
                table: "RcuOutcomes",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "OverrideApproverOfficerId",
                table: "RcuInitiation",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "WaivedByOfficerId",
                table: "Pdd",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ApproverUserId",
                table: "ApprovalDecision",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RecommenderUserId",
                table: "ApprovalDecision",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedOfficerId",
                table: "Applications",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Applications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConcurrencyStamp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RoleId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClaimType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClaimValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UserName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedUserName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedEmail = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SecurityStamp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConcurrencyStamp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumber = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumberConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Branches_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClaimType = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClaimValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderKey = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderDisplayName = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RoleId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoginProvider = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004279",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004282",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004286",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004288",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004290",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004295",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004301",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004306",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004309",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004316",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004319",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004323",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004331",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004336",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004343",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004345",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004351",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004355",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004359",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004364",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004366",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004369",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004371",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004374",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004380",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004382",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004384",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004392",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004397",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004399",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004405",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004412",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004416",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004420",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004423",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004428",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004431",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004434",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004436",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004439",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004441",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004448",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004452",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004456",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004459",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004461",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004464",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004468",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004470",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004478",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004481",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004485",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004487",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004490",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004492",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004500",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004502",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004505",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004513",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004515",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004517",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004522",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004525",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004532",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004540",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004547",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004551",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004555",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004560",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004564",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004571",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004576",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004580",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004582",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004586",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004591",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004598",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004601",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004605",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004610",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004613",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004620",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004624",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004627",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004633",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004638",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004646",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004653",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004656",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004663",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004665",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004670",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004672",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004675",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004677",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004680",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004686",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004693",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004696",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004700",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004707",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004715",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004723",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004725",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004729",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004735",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004743",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004747",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004750",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004754",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004761",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004767",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004770",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004773",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004776",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004782",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004785",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004788",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004790",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004795",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004802",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004811",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004820",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004831",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004844",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004859",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004868",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Applications",
                keyColumn: "Id",
                keyValue: "LN-2026-004871",
                columns: new[] { "AssignedOfficerId", "CompanyId" },
                values: new object[] { null, 1 });

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "CreatedAt", "Name" },
                values: new object[] { 1, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), "Default Company — rename in Company Setup" });

            migrationBuilder.InsertData(
                table: "Branches",
                columns: new[] { "Id", "CompanyId", "Name" },
                values: new object[,]
                {
                    { 1, 1, "Nashik West" },
                    { 2, 1, "Nashik East" },
                    { 3, 1, "Pune Camp" },
                    { 4, 1, "Aurangabad" },
                    { 5, 1, "Jalgaon" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecision_ApproverUserId",
                table: "ApprovalDecision",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecision_RecommenderUserId",
                table: "ApprovalDecision",
                column: "RecommenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CompanyId",
                table: "Applications",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CompanyId",
                table: "AspNetUsers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_CompanyId",
                table: "Branches",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_AspNetUsers_AssignedOfficerId",
                table: "Applications",
                column: "AssignedOfficerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Companies_CompanyId",
                table: "Applications",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalDecision_AspNetUsers_ApproverUserId",
                table: "ApprovalDecision",
                column: "ApproverUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalDecision_AspNetUsers_RecommenderUserId",
                table: "ApprovalDecision",
                column: "RecommenderUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pdd_AspNetUsers_WaivedByOfficerId",
                table: "Pdd",
                column: "WaivedByOfficerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RcuInitiation_AspNetUsers_OverrideApproverOfficerId",
                table: "RcuInitiation",
                column: "OverrideApproverOfficerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RcuOutcomes_AspNetUsers_VerifiedByOfficerId",
                table: "RcuOutcomes",
                column: "VerifiedByOfficerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_AspNetUsers_AssignedOfficerId",
                table: "Applications");

            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Companies_CompanyId",
                table: "Applications");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalDecision_AspNetUsers_ApproverUserId",
                table: "ApprovalDecision");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalDecision_AspNetUsers_RecommenderUserId",
                table: "ApprovalDecision");

            migrationBuilder.DropForeignKey(
                name: "FK_Pdd_AspNetUsers_WaivedByOfficerId",
                table: "Pdd");

            migrationBuilder.DropForeignKey(
                name: "FK_RcuInitiation_AspNetUsers_OverrideApproverOfficerId",
                table: "RcuInitiation");

            migrationBuilder.DropForeignKey(
                name: "FK_RcuOutcomes_AspNetUsers_VerifiedByOfficerId",
                table: "RcuOutcomes");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalDecision_ApproverUserId",
                table: "ApprovalDecision");

            migrationBuilder.DropIndex(
                name: "IX_ApprovalDecision_RecommenderUserId",
                table: "ApprovalDecision");

            migrationBuilder.DropIndex(
                name: "IX_Applications_CompanyId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "ApproverUserId",
                table: "ApprovalDecision");

            migrationBuilder.DropColumn(
                name: "RecommenderUserId",
                table: "ApprovalDecision");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Applications");

            migrationBuilder.AlterColumn<int>(
                name: "VerifiedByOfficerId",
                table: "RcuOutcomes",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "OverrideApproverOfficerId",
                table: "RcuInitiation",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "WaivedByOfficerId",
                table: "Pdd",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ApproverName",
                table: "ApprovalDecision",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RecommenderName",
                table: "ApprovalDecision",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "AssignedOfficerId",
                table: "Applications",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Officers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Officers", x => x.Id);
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

            migrationBuilder.InsertData(
                table: "Officers",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "R. Kulkarni" },
                    { 2, "S. Deshpande" },
                    { 3, "A. Rao" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Officers_AssignedOfficerId",
                table: "Applications",
                column: "AssignedOfficerId",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pdd_Officers_WaivedByOfficerId",
                table: "Pdd",
                column: "WaivedByOfficerId",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RcuInitiation_Officers_OverrideApproverOfficerId",
                table: "RcuInitiation",
                column: "OverrideApproverOfficerId",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RcuOutcomes_Officers_VerifiedByOfficerId",
                table: "RcuOutcomes",
                column: "VerifiedByOfficerId",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

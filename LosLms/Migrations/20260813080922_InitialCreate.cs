using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LosLms.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    ContactEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ContactPhone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SlaOverdueDays = table.Column<int>(type: "INTEGER", nullable: false),
                    FoirCapPct = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    LtvCapPct = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    FoirRiskCautionPct = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    FoirRiskDangerPct = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    LtvRiskCautionPct = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    LtvRiskDangerPct = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    GstPct = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    CibilMinScore = table.Column<int>(type: "INTEGER", nullable: false),
                    CibilMaxScore = table.Column<int>(type: "INTEGER", nullable: false),
                    AddressValidityDays = table.Column<int>(type: "INTEGER", nullable: false),
                    NoteStaleTolerancePct = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    MinimumReferences = table.Column<int>(type: "INTEGER", nullable: false),
                    SetupCompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IfscBankLookup",
                columns: table => new
                {
                    IfscPrefix = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    BankName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IfscBankLookup", x => x.IfscPrefix);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "VehicleLoanCaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Make = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxLoanAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomerType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LoanProduct = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Scheme = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LoanAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Tenure = table.Column<int>(type: "INTEGER", nullable: true),
                    Roi = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    ProcessingFee = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    AdvanceEmi = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    RepaymentMode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    DisbursalDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CurrentStage = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CibilGateStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false, defaultValue: "New"),
                    Disbursed = table.Column<bool>(type: "INTEGER", nullable: false),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SourcingChannel = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    AssignedOfficer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AssignedOfficerId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Applications_AspNetUsers_AssignedOfficerId",
                        column: x => x.AssignedOfficerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Applications_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "AdminRequest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RequestType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RequestedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RequestReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SubjectKey = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReviewNote = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminRequest_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdminRequest_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdminRequest_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalDecision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ApprovalNote = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    RecommenderUserId = table.Column<string>(type: "TEXT", nullable: true),
                    RecommenderRole = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    ApproverUserId = table.Column<string>(type: "TEXT", nullable: true),
                    ApproverRole = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Authority = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    RecommenderDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ApproverDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ValidityDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    SanctionedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    SanctionedRoi = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    SanctionedTenure = table.Column<int>(type: "INTEGER", nullable: true),
                    Conditions = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    SanctionConfirmed = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    SanctionConfirmedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDecision", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalDecision_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalDecision_AspNetUsers_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalDecision_AspNetUsers_RecommenderUserId",
                        column: x => x.RecommenderUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BankName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    BankBranch = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    BankAddress = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    AccountNumber = table.Column<string>(type: "TEXT", maxLength: 18, nullable: true),
                    Ifsc = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    AccountType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    AccountHolderName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Vintage = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PennyDropStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "NotRun"),
                    PennyDropCheckedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankDetails_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankingRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PartyType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Bank = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Months = table.Column<int>(type: "INTEGER", nullable: false),
                    AvgBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Bounces = table.Column<int>(type: "INTEGER", nullable: false),
                    InwardPct = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    OutwardPct = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankingRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankingRecords_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankStatementAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PartyType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "NotConfigured"),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RawResultJson = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankStatementAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankStatementAnalyses_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankStatements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Period = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    UploadedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ParsedStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false, defaultValue: "NotConfigured"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankStatements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankStatements_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Business",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FirmName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Constitution = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Gstin = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Vintage = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    IncorpDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Turnover = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Narrative = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Business", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Business_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CamCostBreakdown",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DraftExShowroomCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    DraftBodyAccessories = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    DraftInsuranceRegistration = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    DraftMargin = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    AppliedExShowroomCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    AppliedBodyAccessories = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    AppliedInsuranceRegistration = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    AppliedMargin = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    LastRecalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CamCostBreakdown", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CamCostBreakdown_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Charges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Head = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Basis = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    DeductedFrom = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Gst = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Locked = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Waived = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    WaiveReason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Charges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Charges_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Classification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Psl = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false, defaultValue: "No - Non-Priority Sector"),
                    PslSub = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    RiskSharing = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    CoLendingPartner = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    EndUse = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    PrioritySectorAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Classification_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Disbursement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    BeneficiaryName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    BeneficiaryAccount = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    PaymentMode = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ValueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Utr = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    FirstEmiDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DisburseFromAccount = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    TradeAdvance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    FirstEmiOverride = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    EmiRoundedTo = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    AgreementFilePath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    AgreementEsignStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "NotSent"),
                    WelcomeLetterFilePath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    WelcomeSmsStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "NotSent"),
                    WelcomeSmsSentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MemoFilePath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    InsuranceFilePath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    GeneralDocFilePath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disbursement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Disbursement_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PartyType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DocumentType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    UploadDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TargetDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ValidityDays = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DownPaymentRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AmountReceived = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ReceiptNo = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    ReceivedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ReceiptFilePath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownPaymentRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DownPaymentRecord_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EligibilityDecision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ApproverNote = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    NoteWrittenAtDeviationPct = table.Column<decimal>(type: "TEXT", precision: 7, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EligibilityDecision", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EligibilityDecision_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnachMandate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Umrn = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    DebitDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    LinkedAccount = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    AccountNumber = table.Column<string>(type: "TEXT", maxLength: 18, nullable: true),
                    Ifsc = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    BankName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    BankBranch = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    NameMatchStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "NotRun"),
                    NameMatchCheckedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConfirmationAccepted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    MandateType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DigitalMode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnachMandate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnachMandate_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExistingLoans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PartyType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Lender = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    LoanType = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Sanctioned = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Pos = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Emi = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Roi = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    MaxDpd = table.Column<int>(type: "INTEGER", nullable: false),
                    Bounces = table.Column<int>(type: "INTEGER", nullable: false),
                    Rtr = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Regular"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExistingLoans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExistingLoans_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Parties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PartyType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    MaritalStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    FatherSpouseName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CustomerCategory = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    Nationality = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    MotherTongue = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Pan = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Aadhaar = table.Column<string>(type: "TEXT", maxLength: 12, nullable: true),
                    PanVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    AadhaarVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    MobileVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    PhotoPath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    PanScanPath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    AadhaarScanPath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    Mobile = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    AltMobile = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Address2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    PinCode = table.Column<string>(type: "TEXT", maxLength: 6, nullable: true),
                    ResidenceType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    YearsAtAddress = table.Column<int>(type: "INTEGER", nullable: true),
                    EmploymentType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    EmployerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Designation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OfficeAddress = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    MonthlyIncome = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    YearsInJob = table.Column<int>(type: "INTEGER", nullable: true),
                    DedupeStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "NotRun"),
                    CibilStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CibilScore = table.Column<int>(type: "INTEGER", nullable: true),
                    CibilCheckedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parties_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Partners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Pan = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Contact = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    Shareholding = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    Dob = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Partners_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pdd",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Item = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Responsible = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Open"),
                    WaivedByOfficerId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pdd", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pdd_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Pdd_AspNetUsers_WaivedByOfficerId",
                        column: x => x.WaivedByOfficerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PostSanctionChecklists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Item = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Flag = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ClearedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostSanctionChecklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostSanctionChecklists_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RcuInitiation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Mode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Screened"),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Vendor = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    InitiationDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CompletionDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Tat = table.Column<int>(type: "INTEGER", nullable: false),
                    CaseRef = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    OverrideActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    OverrideReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OverrideApproverOfficerId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RcuInitiation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RcuInitiation_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RcuInitiation_AspNetUsers_OverrideApproverOfficerId",
                        column: x => x.OverrideApproverOfficerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RcuOutcomes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PartyType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    VerifiedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    VerifiedByOfficerId = table.Column<string>(type: "TEXT", nullable: true),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RcuOutcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RcuOutcomes_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RcuOutcomes_AspNetUsers_VerifiedByOfficerId",
                        column: x => x.VerifiedByOfficerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RcuReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SequenceNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RcuReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RcuReports_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "References",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Relationship = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Mobile = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    KnownSince = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    IdProofFilePath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    PhotoFilePath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "RejectionLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StageAtRejection = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    RejectedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RejectionLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RejectionLog_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecurityDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AssetType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MakeModel = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    MfgYear = table.Column<string>(type: "TEXT", maxLength: 4, nullable: true),
                    RegNo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    ChassisNo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    EngineNo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    InvoiceNo = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    InvoiceDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    InvoiceValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    InsuranceProvider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PolicyNo = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    PolicyExpiry = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PropertyType = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    PropertyAddress = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    Area = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    OwnershipType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    SaleDeedNo = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    ValuationRefNo = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    EncumbranceRef = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    AssessedValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    InvoiceFilePath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    InsuranceFilePath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "SecurityNachMandate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Umrn = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    MandateHolder = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    AccountNumber = table.Column<string>(type: "TEXT", maxLength: 18, nullable: true),
                    Ifsc = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    BankName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    BankBranch = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    NameMatchStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "NotRun"),
                    NameMatchCheckedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConfirmationAccepted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    MandateType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DigitalMode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityNachMandate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityNachMandate_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SendBackLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FromStage = table.Column<int>(type: "INTEGER", nullable: false),
                    ToStage = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SendBackLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SendBackLog_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tvr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Agent = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    PersonContacted = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Relationship = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RecordingRef = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    CallDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tvr", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tvr_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Viability",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IncomeFreight = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    IncomeSalary = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    IncomeOther = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    ExpenseHousehold = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    ExpenseFuelDriver = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    ExpenseExistingEmi = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "DocumentRemarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRemarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentRemarks_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "Address", "AddressValidityDays", "CibilMaxScore", "CibilMinScore", "ContactEmail", "ContactPhone", "CreatedAt", "FoirCapPct", "FoirRiskCautionPct", "FoirRiskDangerPct", "GstPct", "LtvCapPct", "LtvRiskCautionPct", "LtvRiskDangerPct", "MinimumReferences", "Name", "NoteStaleTolerancePct", "SetupCompletedAt", "SlaOverdueDays", "UpdatedAt" },
                values: new object[] { 1, null, 90, 900, 300, null, null, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc), 50m, 40m, 60m, 18m, 85m, 75m, 90m, 2, "", 0.5m, null, 5, new DateTime(2026, 8, 11, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "IfscBankLookup",
                columns: new[] { "IfscPrefix", "BankName" },
                values: new object[,]
                {
                    { "BARB", "Bank of Baroda" },
                    { "CNRB", "Canara Bank" },
                    { "HDFC", "HDFC Bank" },
                    { "ICIC", "ICICI Bank" },
                    { "IDFB", "IDFC First Bank" },
                    { "INDB", "IndusInd Bank" },
                    { "KKBK", "Kotak Mahindra Bank" },
                    { "PUNB", "Punjab National Bank" },
                    { "SBIN", "State Bank of India" },
                    { "UBIN", "Union Bank of India" },
                    { "UTIB", "Axis Bank" },
                    { "YESB", "Yes Bank" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminRequest_ApplicationId_RequestType_Status",
                table: "AdminRequest",
                columns: new[] { "ApplicationId", "RequestType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminRequest_RequestedByUserId",
                table: "AdminRequest",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminRequest_ReviewedByUserId",
                table: "AdminRequest",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminRequest_Status",
                table: "AdminRequest",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_AssignedOfficerId",
                table: "Applications",
                column: "AssignedOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CompanyId",
                table: "Applications",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecision_ApplicationId",
                table: "ApprovalDecision",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecision_ApproverUserId",
                table: "ApprovalDecision",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDecision_RecommenderUserId",
                table: "ApprovalDecision",
                column: "RecommenderUserId");

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
                name: "IX_BankDetails_ApplicationId",
                table: "BankDetails",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankingRecords_ApplicationId",
                table: "BankingRecords",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementAnalyses_ApplicationId",
                table: "BankStatementAnalyses",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatements_ApplicationId",
                table: "BankStatements",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_CompanyId",
                table: "Branches",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Business_ApplicationId",
                table: "Business",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CamCostBreakdown_ApplicationId",
                table: "CamCostBreakdown",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Charges_ApplicationId",
                table: "Charges",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Classification_ApplicationId",
                table: "Classification",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Disbursement_ApplicationId",
                table: "Disbursement",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRemarks_DocumentId",
                table: "DocumentRemarks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ApplicationId_PartyType_DocumentType",
                table: "Documents",
                columns: new[] { "ApplicationId", "PartyType", "DocumentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DownPaymentRecord_ApplicationId",
                table: "DownPaymentRecord",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EligibilityDecision_ApplicationId",
                table: "EligibilityDecision",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnachMandate_ApplicationId",
                table: "EnachMandate",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExistingLoans_ApplicationId",
                table: "ExistingLoans",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_ApplicationId_PartyType",
                table: "Parties",
                columns: new[] { "ApplicationId", "PartyType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partners_ApplicationId",
                table: "Partners",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Pdd_ApplicationId",
                table: "Pdd",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Pdd_WaivedByOfficerId",
                table: "Pdd",
                column: "WaivedByOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_PostSanctionChecklists_ApplicationId",
                table: "PostSanctionChecklists",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_RcuInitiation_ApplicationId",
                table: "RcuInitiation",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RcuInitiation_OverrideApproverOfficerId",
                table: "RcuInitiation",
                column: "OverrideApproverOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_RcuOutcomes_ApplicationId_PartyType",
                table: "RcuOutcomes",
                columns: new[] { "ApplicationId", "PartyType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RcuOutcomes_VerifiedByOfficerId",
                table: "RcuOutcomes",
                column: "VerifiedByOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_RcuReports_ApplicationId_SequenceNumber",
                table: "RcuReports",
                columns: new[] { "ApplicationId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_References_ApplicationId",
                table: "References",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_RejectionLog_ApplicationId",
                table: "RejectionLog",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityDetails_ApplicationId",
                table: "SecurityDetails",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityNachMandate_ApplicationId",
                table: "SecurityNachMandate",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SendBackLog_ApplicationId",
                table: "SendBackLog",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Tvr_ApplicationId",
                table: "Tvr",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleLoanCaps_CompanyId_Make_Model_Year",
                table: "VehicleLoanCaps",
                columns: new[] { "CompanyId", "Make", "Model", "Year" },
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
                name: "AdminRequest");

            migrationBuilder.DropTable(
                name: "ApprovalDecision");

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
                name: "BankDetails");

            migrationBuilder.DropTable(
                name: "BankingRecords");

            migrationBuilder.DropTable(
                name: "BankStatementAnalyses");

            migrationBuilder.DropTable(
                name: "BankStatements");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Business");

            migrationBuilder.DropTable(
                name: "CamCostBreakdown");

            migrationBuilder.DropTable(
                name: "Charges");

            migrationBuilder.DropTable(
                name: "Classification");

            migrationBuilder.DropTable(
                name: "Disbursement");

            migrationBuilder.DropTable(
                name: "DocumentRemarks");

            migrationBuilder.DropTable(
                name: "DownPaymentRecord");

            migrationBuilder.DropTable(
                name: "EligibilityDecision");

            migrationBuilder.DropTable(
                name: "EnachMandate");

            migrationBuilder.DropTable(
                name: "ExistingLoans");

            migrationBuilder.DropTable(
                name: "IfscBankLookup");

            migrationBuilder.DropTable(
                name: "Parties");

            migrationBuilder.DropTable(
                name: "Partners");

            migrationBuilder.DropTable(
                name: "Pdd");

            migrationBuilder.DropTable(
                name: "PostSanctionChecklists");

            migrationBuilder.DropTable(
                name: "RcuInitiation");

            migrationBuilder.DropTable(
                name: "RcuOutcomes");

            migrationBuilder.DropTable(
                name: "RcuReports");

            migrationBuilder.DropTable(
                name: "References");

            migrationBuilder.DropTable(
                name: "RejectionLog");

            migrationBuilder.DropTable(
                name: "SecurityDetails");

            migrationBuilder.DropTable(
                name: "SecurityNachMandate");

            migrationBuilder.DropTable(
                name: "SendBackLog");

            migrationBuilder.DropTable(
                name: "Tvr");

            migrationBuilder.DropTable(
                name: "VehicleLoanCaps");

            migrationBuilder.DropTable(
                name: "Viability");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}

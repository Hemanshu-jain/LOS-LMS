using System;
using Microsoft.EntityFrameworkCore.Metadata;
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
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Branch = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoanProduct = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Scheme = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoanAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Tenure = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Roi = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentStage = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "New")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourcingChannel = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AssignedOfficer = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Parties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ApplicationId = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PartyType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FullName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaritalStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FatherSpouseName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerCategory = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nationality = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MotherTongue = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Pan = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Aadhaar = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PanVerified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AadhaarVerified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MobileVerified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PhotoPath = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PanScanPath = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AadhaarScanPath = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mobile = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AltMobile = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address1 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address2 = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    City = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    State = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PinCode = table.Column<string>(type: "varchar(6)", maxLength: 6, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResidenceType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    YearsAtAddress = table.Column<int>(type: "int", nullable: true),
                    EmploymentType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmployerName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Designation = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OfficeAddress = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MonthlyIncome = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    YearsInJob = table.Column<int>(type: "int", nullable: true),
                    DedupeStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "NotRun")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Applications",
                columns: new[] { "Id", "AssignedOfficer", "Branch", "CreatedAt", "CurrentStage", "CustomerName", "CustomerType", "LoanAmount", "LoanProduct", "Roi", "Scheme", "SourcingChannel", "Status", "Tenure", "UpdatedAt" },
                values: new object[,]
                {
                    { "LN-2026-004279", "A. Rao", "Nashik East", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Trupti Thorat", "Individual · CV", 4630000m, "Commercial vehicle", "13.50%", "CV-STD-2026", "Branch walk-in", "In progress", "72 mo", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004282", "R. Kulkarni", "Jalgaon", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Vitthal Carriers", "Individual · CV", 1370000m, "Commercial vehicle", "14.25%", "CV-STD-2026", "Branch walk-in", "In progress", "60 mo", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004286", "S. Deshpande", "Nashik East", new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), 7, "Jai Malhar Logistics", "Individual · CV", 3800000m, "Commercial vehicle", "13.00%", "CV-STD-2026", "Branch walk-in", "In progress", "60 mo", new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004288", "R. Kulkarni", "Nashik East", new DateTime(2026, 5, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), 7, "Vaishali Khedkar", "Individual · CV", 1560000m, "Commercial vehicle", "13.50%", "CV-STD-2026", "Digital", "In progress", "36 mo", new DateTime(2026, 5, 24, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004290", "R. Kulkarni", "Jalgaon", new DateTime(2026, 5, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Sahyadri Transport Co", "Individual · CV", 1110000m, "Commercial vehicle", "14.00%", "CV-STD-2026", "Digital", "New", "60 mo", new DateTime(2026, 5, 25, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004295", "A. Rao", "Pune Camp", new DateTime(2026, 5, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Vikram Kulkarni", "Individual · LAP", 1080000m, "Loan against property", "13.00%", "LAP-STD-2026", "DSA", "Rejected", "72 mo", new DateTime(2026, 5, 25, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004301", "S. Deshpande", "Aurangabad", new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Farhan Gaikwad", "Individual · LAP", 4430000m, "Loan against property", "14.50%", "LAP-STD-2026", "Branch walk-in", "In progress", "48 mo", new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004306", "A. Rao", "Nashik East", new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Smita Bhosale", "Individual · LAP", 3890000m, "Loan against property", "13.50%", "LAP-STD-2026", "Branch walk-in", "Rejected", "60 mo", new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004309", "R. Kulkarni", "Nashik East", new DateTime(2026, 5, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Yogesh Shinde", "Individual · CV", 2290000m, "Commercial vehicle", "14.25%", "CV-STD-2026", "Branch walk-in", "In progress", "48 mo", new DateTime(2026, 5, 27, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004316", "R. Kulkarni", "Nashik West", new DateTime(2026, 5, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Vaishali Nikam", "Individual · LAP", 4290000m, "Loan against property", "13.75%", "LAP-STD-2026", "Digital", "In progress", "36 mo", new DateTime(2026, 5, 28, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004319", "A. Rao", "Nashik East", new DateTime(2026, 5, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Ganraj Transports", "Individual · CV", 1490000m, "Commercial vehicle", "14.50%", "CV-STD-2026", "DSA", "Sanctioned", "72 mo", new DateTime(2026, 5, 29, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004323", "S. Deshpande", "Pune Camp", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Farhan Salunkhe", "Individual · LAP", 2510000m, "Loan against property", "13.25%", "LAP-STD-2026", "Branch walk-in", "Rejected", "72 mo", new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004331", "A. Rao", "Jalgaon", new DateTime(2026, 5, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Nitin Deshmukh", "Individual · LAP", 2390000m, "Loan against property", "13.00%", "LAP-STD-2026", "DSA", "In progress", "72 mo", new DateTime(2026, 5, 31, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004336", "A. Rao", "Jalgaon", new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Shalini Nikam", "Individual · CV", 4640000m, "Commercial vehicle", "14.25%", "CV-STD-2026", "DSA", "In progress", "36 mo", new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004343", "R. Kulkarni", "Nashik West", new DateTime(2026, 6, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Vitthal Carriers", "Individual · CV", 4340000m, "Commercial vehicle", "13.75%", "CV-STD-2026", "DSA", "In progress", "72 mo", new DateTime(2026, 6, 3, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004345", "A. Rao", "Nashik West", new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Rekha Rao", "Individual · LAP", 4680000m, "Loan against property", "14.25%", "LAP-STD-2026", "Branch walk-in", "Sanctioned", "48 mo", new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004351", "A. Rao", "Nashik West", new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Jyoti Kulkarni", "Individual · LAP", 1670000m, "Loan against property", "13.50%", "LAP-STD-2026", "DSA", "In progress", "72 mo", new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004355", "R. Kulkarni", "Jalgaon", new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Vikram Patil", "Individual · LAP", 4680000m, "Loan against property", "14.50%", "LAP-STD-2026", "Digital", "In progress", "72 mo", new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004359", "S. Deshpande", "Nashik East", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Ganesh Kadam", "Individual · CV", 3990000m, "Commercial vehicle", "13.25%", "CV-STD-2026", "DSA", "In progress", "72 mo", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004364", "A. Rao", "Jalgaon", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Balaji Carriers", "Individual · CV", 1220000m, "Commercial vehicle", "14.50%", "CV-STD-2026", "DSA", "In progress", "36 mo", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004366", "R. Kulkarni", "Nashik West", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Yogesh Deshmukh", "Individual · CV", 3630000m, "Commercial vehicle", "12.75%", "CV-STD-2026", "DSA", "New", "60 mo", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004369", "S. Deshpande", "Jalgaon", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Sahyadri Transport Co", "Individual · CV", 1230000m, "Commercial vehicle", "13.50%", "CV-STD-2026", "Branch walk-in", "In progress", "48 mo", new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004371", "R. Kulkarni", "Aurangabad", new DateTime(2026, 6, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Meena Jadhav", "Individual · CV", 4500000m, "Commercial vehicle", "14.00%", "CV-STD-2026", "Branch walk-in", "New", "72 mo", new DateTime(2026, 6, 9, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004374", "S. Deshpande", "Nashik West", new DateTime(2026, 6, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Mahesh More", "Individual · CV", 2170000m, "Commercial vehicle", "12.50%", "CV-STD-2026", "DSA", "In progress", "60 mo", new DateTime(2026, 6, 9, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004380", "S. Deshpande", "Jalgaon", new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Vaishali Salunkhe", "Individual · CV", 2450000m, "Commercial vehicle", "14.00%", "CV-STD-2026", "Digital", "In progress", "36 mo", new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004382", "S. Deshpande", "Aurangabad", new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Dattatray Jadhav", "Individual · LAP", 3770000m, "Loan against property", "14.50%", "LAP-STD-2026", "Digital", "Sanctioned", "60 mo", new DateTime(2026, 6, 13, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004384", "S. Deshpande", "Nashik West", new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Ganraj Transports", "Individual · CV", 2560000m, "Commercial vehicle", "12.75%", "CV-STD-2026", "Branch walk-in", "In progress", "36 mo", new DateTime(2026, 6, 19, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004392", "S. Deshpande", "Pune Camp", new DateTime(2026, 6, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Ajay Dhumal", "Individual · LAP", 2180000m, "Loan against property", "12.50%", "LAP-STD-2026", "DSA", "In progress", "36 mo", new DateTime(2026, 6, 20, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004397", "S. Deshpande", "Jalgaon", new DateTime(2026, 6, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), 7, "Pooja Bhosale", "Individual · CV", 1640000m, "Commercial vehicle", "13.75%", "CV-STD-2026", "DSA", "In progress", "48 mo", new DateTime(2026, 6, 21, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004399", "A. Rao", "Pune Camp", new DateTime(2026, 6, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Pooja Kulkarni", "Individual · CV", 4670000m, "Commercial vehicle", "14.25%", "CV-STD-2026", "Branch walk-in", "New", "72 mo", new DateTime(2026, 6, 21, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004405", "S. Deshpande", "Nashik East", new DateTime(2026, 6, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Shalini Bhosale", "Individual · LAP", 1450000m, "Loan against property", "13.50%", "LAP-STD-2026", "DSA", "Sanctioned", "48 mo", new DateTime(2026, 6, 22, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004412", "R. Kulkarni", "Aurangabad", new DateTime(2026, 6, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Maratha Roadways", "Individual · CV", 2600000m, "Commercial vehicle", "13.00%", "CV-STD-2026", "Branch walk-in", "In progress", "36 mo", new DateTime(2026, 6, 25, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004416", "A. Rao", "Aurangabad", new DateTime(2026, 6, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), 7, "Farhan Waghmare", "Individual · LAP", 2710000m, "Loan against property", "14.00%", "LAP-STD-2026", "Digital", "In progress", "36 mo", new DateTime(2026, 6, 26, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004420", "A. Rao", "Pune Camp", new DateTime(2026, 6, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Manisha Joshi", "Individual · LAP", 2240000m, "Loan against property", "12.75%", "LAP-STD-2026", "Digital", "In progress", "36 mo", new DateTime(2026, 6, 29, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004423", "A. Rao", "Pune Camp", new DateTime(2026, 6, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Sachin Nikam", "Individual · CV", 4030000m, "Commercial vehicle", "13.50%", "CV-STD-2026", "Digital", "Rejected", "72 mo", new DateTime(2026, 6, 29, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004428", "S. Deshpande", "Aurangabad", new DateTime(2026, 6, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Sahyadri Transport Co", "Individual · CV", 1660000m, "Commercial vehicle", "12.50%", "CV-STD-2026", "Branch walk-in", "In progress", "36 mo", new DateTime(2026, 6, 29, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004431", "A. Rao", "Pune Camp", new DateTime(2026, 6, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Anil Gaikwad", "Individual · LAP", 1600000m, "Loan against property", "13.25%", "LAP-STD-2026", "DSA", "In progress", "72 mo", new DateTime(2026, 6, 29, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004434", "S. Deshpande", "Pune Camp", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 7, "Nilima Sawant", "Individual · CV", 3270000m, "Commercial vehicle", "14.00%", "CV-STD-2026", "DSA", "In progress", "60 mo", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004436", "R. Kulkarni", "Jalgaon", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Amol Bhalerao", "Individual · CV", 930000m, "Commercial vehicle", "14.50%", "CV-STD-2026", "Branch walk-in", "Sanctioned", "36 mo", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004439", "A. Rao", "Nashik East", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Konkan Freight Movers", "Individual · CV", 1630000m, "Commercial vehicle", "13.00%", "CV-STD-2026", "Branch walk-in", "New", "72 mo", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004441", "R. Kulkarni", "Nashik West", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Pooja Thorat", "Individual · CV", 1110000m, "Commercial vehicle", "13.50%", "CV-STD-2026", "Digital", "In progress", "48 mo", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004448", "A. Rao", "Jalgaon", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Dattatray Gaikwad", "Individual · LAP", 1790000m, "Loan against property", "13.00%", "LAP-STD-2026", "Digital", "New", "36 mo", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004452", "R. Kulkarni", "Pune Camp", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Sachin Nikam", "Individual · LAP", 4740000m, "Loan against property", "14.00%", "LAP-STD-2026", "DSA", "New", "36 mo", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004456", "A. Rao", "Nashik West", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Rekha Rao", "Individual · LAP", 930000m, "Loan against property", "12.75%", "LAP-STD-2026", "Branch walk-in", "New", "36 mo", new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004459", "R. Kulkarni", "Jalgaon", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Meena Deshmukh", "Individual · LAP", 2620000m, "Loan against property", "13.50%", "LAP-STD-2026", "Digital", "In progress", "72 mo", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004461", "S. Deshpande", "Jalgaon", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Deepa Patil", "Individual · LAP", 3630000m, "Loan against property", "14.00%", "LAP-STD-2026", "Branch walk-in", "In progress", "48 mo", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004464", "A. Rao", "Nashik West", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Mahesh Ingle", "Individual · LAP", 4500000m, "Loan against property", "12.50%", "LAP-STD-2026", "Branch walk-in", "In progress", "36 mo", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004468", "S. Deshpande", "Pune Camp", new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Balaji Carriers", "Individual · CV", 4550000m, "Commercial vehicle", "13.50%", "CV-STD-2026", "Digital", "Sanctioned", "36 mo", new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004470", "A. Rao", "Aurangabad", new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Ganesh Bhalerao", "Individual · LAP", 2080000m, "Loan against property", "14.00%", "LAP-STD-2026", "Digital", "In progress", "60 mo", new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004478", "S. Deshpande", "Jalgaon", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Sai Logistics", "Individual · CV", 3380000m, "Commercial vehicle", "13.75%", "CV-STD-2026", "DSA", "In progress", "60 mo", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004481", "S. Deshpande", "Nashik West", new DateTime(2026, 7, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Deepa Sawant", "Individual · LAP", 1600000m, "Loan against property", "14.50%", "LAP-STD-2026", "DSA", "Rejected", "48 mo", new DateTime(2026, 7, 4, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004485", "R. Kulkarni", "Nashik East", new DateTime(2026, 7, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Iqbal Transport LLP", "Individual · CV", 4380000m, "Commercial vehicle", "13.25%", "CV-STD-2026", "DSA", "In progress", "48 mo", new DateTime(2026, 7, 4, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004487", "S. Deshpande", "Nashik East", new DateTime(2026, 7, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Panchvati Freight", "Individual · CV", 2990000m, "Commercial vehicle", "13.75%", "CV-STD-2026", "DSA", "Sanctioned", "72 mo", new DateTime(2026, 7, 4, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004490", "S. Deshpande", "Nashik East", new DateTime(2026, 7, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Meena Jadhav", "Individual · LAP", 4790000m, "Loan against property", "14.50%", "LAP-STD-2026", "Branch walk-in", "In progress", "60 mo", new DateTime(2026, 7, 4, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004492", "S. Deshpande", "Jalgaon", new DateTime(2026, 7, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Shree Roadlines", "Individual · CV", 3520000m, "Commercial vehicle", "12.75%", "CV-STD-2026", "Digital", "Rejected", "36 mo", new DateTime(2026, 7, 5, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004500", "S. Deshpande", "Nashik East", new DateTime(2026, 7, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Manisha Dhumal", "Individual · LAP", 1780000m, "Loan against property", "12.50%", "LAP-STD-2026", "Digital", "In progress", "36 mo", new DateTime(2026, 7, 5, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004502", "A. Rao", "Aurangabad", new DateTime(2026, 7, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Deccan Cargo LLP", "Individual · CV", 2230000m, "Commercial vehicle", "13.00%", "CV-STD-2026", "Digital", "Sanctioned", "60 mo", new DateTime(2026, 7, 5, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004505", "S. Deshpande", "Jalgaon", new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Sachin Chavan", "Individual · CV", 4020000m, "Commercial vehicle", "13.75%", "CV-STD-2026", "Branch walk-in", "In progress", "48 mo", new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004513", "R. Kulkarni", "Nashik East", new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), 7, "Rekha Salunkhe", "Individual · LAP", 3830000m, "Loan against property", "13.50%", "LAP-STD-2026", "Digital", "In progress", "48 mo", new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004515", "S. Deshpande", "Jalgaon", new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Sunil Shinde", "Individual · LAP", 1570000m, "Loan against property", "14.00%", "LAP-STD-2026", "DSA", "In progress", "72 mo", new DateTime(2026, 7, 6, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004517", "A. Rao", "Pune Camp", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Meena Kadam", "Individual · CV", 2210000m, "Commercial vehicle", "14.50%", "CV-STD-2026", "DSA", "New", "48 mo", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004522", "R. Kulkarni", "Nashik East", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Sachin Thorat", "Individual · CV", 4280000m, "Commercial vehicle", "13.50%", "CV-STD-2026", "DSA", "In progress", "60 mo", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004525", "R. Kulkarni", "Pune Camp", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Ajay Pawar", "Individual · CV", 1700000m, "Commercial vehicle", "14.25%", "CV-STD-2026", "Digital", "Sanctioned", "48 mo", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004532", "S. Deshpande", "Nashik East", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Panchvati Freight", "Individual · CV", 960000m, "Commercial vehicle", "13.75%", "CV-STD-2026", "DSA", "New", "36 mo", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004540", "A. Rao", "Nashik West", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Kavita Salunkhe", "Individual · LAP", 3610000m, "Loan against property", "13.50%", "LAP-STD-2026", "Branch walk-in", "Sanctioned", "36 mo", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004547", "S. Deshpande", "Jalgaon", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Sanjay Dhumal", "Individual · LAP", 2960000m, "Loan against property", "13.00%", "LAP-STD-2026", "DSA", "New", "72 mo", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004551", "A. Rao", "Pune Camp", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Sahyadri Transport Co", "Individual · CV", 2810000m, "Commercial vehicle", "14.00%", "CV-STD-2026", "Branch walk-in", "In progress", "72 mo", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004555", "R. Kulkarni", "Nashik East", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Sanjay Sawant", "Individual · LAP", 2930000m, "Loan against property", "12.75%", "LAP-STD-2026", "DSA", "Sanctioned", "72 mo", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004560", "R. Kulkarni", "Nashik West", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Vitthal Carriers", "Individual · CV", 760000m, "Commercial vehicle", "14.00%", "CV-STD-2026", "DSA", "Sanctioned", "36 mo", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004564", "R. Kulkarni", "Pune Camp", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Sunil Thorat", "Individual · LAP", 2860000m, "Loan against property", "12.75%", "LAP-STD-2026", "Digital", "In progress", "36 mo", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004571", "A. Rao", "Nashik West", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Vikram Bhosale", "Individual · CV", 1280000m, "Commercial vehicle", "14.50%", "CV-STD-2026", "Digital", "In progress", "72 mo", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004576", "A. Rao", "Nashik East", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Anil Mane", "Individual · LAP", 2730000m, "Loan against property", "13.50%", "LAP-STD-2026", "Branch walk-in", "In progress", "36 mo", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004580", "R. Kulkarni", "Pune Camp", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Jyoti Salunkhe", "Individual · CV", 1890000m, "Commercial vehicle", "14.50%", "CV-STD-2026", "Branch walk-in", "Rejected", "36 mo", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004582", "R. Kulkarni", "Jalgaon", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Vaishali Joshi", "Individual · LAP", 4060000m, "Loan against property", "12.75%", "LAP-STD-2026", "Branch walk-in", "In progress", "60 mo", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004586", "R. Kulkarni", "Nashik West", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Nitin Kadam", "Individual · CV", 3390000m, "Commercial vehicle", "13.75%", "CV-STD-2026", "Digital", "In progress", "60 mo", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004591", "A. Rao", "Nashik West", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Ramesh Sawant", "Individual · LAP", 3950000m, "Loan against property", "12.75%", "LAP-STD-2026", "Digital", "In progress", "72 mo", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004598", "A. Rao", "Aurangabad", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Godavari Movers", "Individual · CV", 4780000m, "Commercial vehicle", "14.50%", "CV-STD-2026", "DSA", "In progress", "60 mo", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004601", "A. Rao", "Nashik East", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 7, "Mahesh Gaikwad", "Individual · LAP", 2370000m, "Loan against property", "13.00%", "LAP-STD-2026", "DSA", "In progress", "48 mo", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004605", "S. Deshpande", "Jalgaon", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Sai Logistics", "Individual · CV", 2880000m, "Commercial vehicle", "14.00%", "CV-STD-2026", "Digital", "In progress", "48 mo", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004610", "A. Rao", "Jalgaon", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Nilima Mane", "Individual · CV", 2210000m, "Commercial vehicle", "13.00%", "CV-STD-2026", "DSA", "In progress", "60 mo", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004613", "R. Kulkarni", "Jalgaon", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Archana Thorat", "Individual · LAP", 1000000m, "Loan against property", "13.75%", "LAP-STD-2026", "Digital", "Sanctioned", "48 mo", new DateTime(2026, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004620", "S. Deshpande", "Aurangabad", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Rahul Khedkar", "Individual · LAP", 3690000m, "Loan against property", "13.25%", "LAP-STD-2026", "Digital", "New", "36 mo", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004624", "R. Kulkarni", "Nashik West", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Jyoti Waghmare", "Individual · LAP", 1740000m, "Loan against property", "14.25%", "LAP-STD-2026", "Branch walk-in", "Sanctioned", "36 mo", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004627", "A. Rao", "Aurangabad", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Shree Roadlines", "Individual · CV", 2510000m, "Commercial vehicle", "12.75%", "CV-STD-2026", "Digital", "Rejected", "72 mo", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004633", "R. Kulkarni", "Jalgaon", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Rekha Joshi", "Individual · LAP", 4290000m, "Loan against property", "14.25%", "LAP-STD-2026", "Branch walk-in", "Rejected", "48 mo", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004638", "R. Kulkarni", "Jalgaon", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Meena Jadhav", "Individual · CV", 3050000m, "Commercial vehicle", "13.25%", "CV-STD-2026", "Branch walk-in", "Rejected", "60 mo", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004646", "A. Rao", "Jalgaon", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Rahul Rao", "Individual · LAP", 1730000m, "Loan against property", "13.00%", "LAP-STD-2026", "Branch walk-in", "In progress", "60 mo", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004653", "R. Kulkarni", "Pune Camp", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 7, "Vaishali Gaikwad", "Individual · LAP", 850000m, "Loan against property", "12.50%", "LAP-STD-2026", "Digital", "In progress", "48 mo", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004656", "A. Rao", "Pune Camp", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Amol Khedkar", "Individual · CV", 3650000m, "Commercial vehicle", "13.25%", "CV-STD-2026", "DSA", "Sanctioned", "36 mo", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004663", "A. Rao", "Pune Camp", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Ajay Nikam", "Individual · CV", 3240000m, "Commercial vehicle", "12.75%", "CV-STD-2026", "Branch walk-in", "In progress", "72 mo", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004665", "R. Kulkarni", "Nashik East", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Pooja Patil", "Individual · LAP", 3970000m, "Loan against property", "13.25%", "LAP-STD-2026", "Digital", "In progress", "48 mo", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004670", "A. Rao", "Jalgaon", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 7, "Kavita Patil", "Individual · LAP", 2220000m, "Loan against property", "14.50%", "LAP-STD-2026", "Digital", "In progress", "60 mo", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004672", "S. Deshpande", "Nashik East", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Manisha Chavan", "Individual · CV", 4620000m, "Commercial vehicle", "12.75%", "CV-STD-2026", "DSA", "In progress", "36 mo", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004675", "A. Rao", "Aurangabad", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Vikram Bhosale", "Individual · LAP", 920000m, "Loan against property", "13.50%", "LAP-STD-2026", "Digital", "Sanctioned", "72 mo", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004677", "R. Kulkarni", "Jalgaon", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Vaishali Rao", "Individual · LAP", 2710000m, "Loan against property", "14.00%", "LAP-STD-2026", "Digital", "In progress", "48 mo", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004680", "R. Kulkarni", "Pune Camp", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Pooja Ingle", "Individual · LAP", 2680000m, "Loan against property", "12.50%", "LAP-STD-2026", "Digital", "In progress", "36 mo", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004686", "S. Deshpande", "Pune Camp", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Sahyadri Transport Co", "Individual · CV", 1290000m, "Commercial vehicle", "14.00%", "CV-STD-2026", "Digital", "In progress", "60 mo", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004693", "R. Kulkarni", "Jalgaon", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Ramesh More", "Individual · CV", 1640000m, "Commercial vehicle", "13.50%", "CV-STD-2026", "DSA", "In progress", "48 mo", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004696", "A. Rao", "Nashik West", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Nitin Deshmukh", "Individual · LAP", 4390000m, "Loan against property", "14.25%", "LAP-STD-2026", "Digital", "In progress", "36 mo", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004700", "A. Rao", "Pune Camp", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Nilima Salunkhe", "Individual · LAP", 1810000m, "Loan against property", "13.00%", "LAP-STD-2026", "Digital", "In progress", "36 mo", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004707", "R. Kulkarni", "Nashik West", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Sanjay Chavan", "Individual · LAP", 1020000m, "Loan against property", "12.50%", "LAP-STD-2026", "Branch walk-in", "In progress", "72 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004715", "R. Kulkarni", "Nashik East", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Trupti Shinde", "Individual · LAP", 860000m, "Loan against property", "14.50%", "LAP-STD-2026", "Branch walk-in", "In progress", "72 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004723", "A. Rao", "Aurangabad", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Shalini Nikam", "Individual · CV", 4350000m, "Commercial vehicle", "14.25%", "CV-STD-2026", "Branch walk-in", "Sanctioned", "72 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004725", "S. Deshpande", "Pune Camp", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Ganesh Nikam", "Individual · CV", 4290000m, "Commercial vehicle", "12.50%", "CV-STD-2026", "Digital", "In progress", "48 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004729", "A. Rao", "Nashik East", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Iqbal Transport LLP", "Individual · CV", 4040000m, "Commercial vehicle", "13.50%", "CV-STD-2026", "Digital", "In progress", "48 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004735", "A. Rao", "Aurangabad", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 7, "Kavita Ingle", "Individual · LAP", 3130000m, "Loan against property", "12.75%", "LAP-STD-2026", "Digital", "In progress", "72 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004743", "S. Deshpande", "Jalgaon", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Dattatray Dhumal", "Individual · LAP", 1240000m, "Loan against property", "12.50%", "LAP-STD-2026", "Branch walk-in", "In progress", "72 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004747", "S. Deshpande", "Pune Camp", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Farhan Waghmare", "Individual · LAP", 1850000m, "Loan against property", "13.50%", "LAP-STD-2026", "Digital", "In progress", "72 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004750", "R. Kulkarni", "Pune Camp", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Manisha Rao", "Individual · LAP", 3360000m, "Loan against property", "14.25%", "LAP-STD-2026", "DSA", "Rejected", "60 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004754", "A. Rao", "Nashik West", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Mahesh Pawar", "Individual · CV", 3090000m, "Commercial vehicle", "13.00%", "CV-STD-2026", "DSA", "In progress", "60 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004761", "R. Kulkarni", "Jalgaon", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Sanjay Nikam", "Individual · LAP", 3950000m, "Loan against property", "12.50%", "LAP-STD-2026", "Branch walk-in", "New", "48 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004767", "A. Rao", "Jalgaon", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Balaji Carriers", "Individual · CV", 1140000m, "Commercial vehicle", "14.00%", "CV-STD-2026", "Branch walk-in", "In progress", "72 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004770", "R. Kulkarni", "Nashik West", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Trupti More", "Individual · LAP", 3340000m, "Loan against property", "12.50%", "LAP-STD-2026", "Digital", "In progress", "60 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004773", "A. Rao", "Nashik West", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Kavita Rao", "Individual · LAP", 3970000m, "Loan against property", "13.25%", "LAP-STD-2026", "DSA", "New", "48 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004776", "S. Deshpande", "Aurangabad", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Mahesh Pawar", "Individual · CV", 3050000m, "Commercial vehicle", "14.00%", "CV-STD-2026", "Digital", "In progress", "36 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004782", "S. Deshpande", "Nashik West", new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Trupti Patil", "Individual · CV", 2310000m, "Commercial vehicle", "13.25%", "CV-STD-2026", "DSA", "In progress", "60 mo", new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004785", "S. Deshpande", "Pune Camp", new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Meena Nikam", "Individual · CV", 2520000m, "Commercial vehicle", "14.00%", "CV-STD-2026", "Digital", "In progress", "48 mo", new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004788", "A. Rao", "Pune Camp", new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Jyoti Waghmare", "Individual · LAP", 2490000m, "Loan against property", "12.50%", "LAP-STD-2026", "DSA", "In progress", "36 mo", new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004790", "R. Kulkarni", "Nashik West", new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Rahul Rao", "Individual · CV", 2400000m, "Commercial vehicle", "13.00%", "CV-STD-2026", "Branch walk-in", "New", "60 mo", new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004795", "A. Rao", "Pune Camp", new DateTime(2026, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Vikram Rao", "Individual · CV", 1420000m, "Commercial vehicle", "14.25%", "CV-STD-2026", "Branch walk-in", "Rejected", "72 mo", new DateTime(2026, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004802", "S. Deshpande", "Aurangabad", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, "Farhan Shaikh", "Individual · LAP", 3750000m, "Loan against property", "13.75%", "LAP-STD-2026", "DSA", "In progress", "60 mo", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004811", "R. Kulkarni", "Nashik East", new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Balaji Carriers", "Individual · CV", 1990000m, "Commercial vehicle", "13.75%", "CV-STD-2026", "Digital", "New", "72 mo", new DateTime(2026, 7, 2, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004820", "A. Rao", "Pune Camp", new DateTime(2026, 7, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, "Meena Kulkarni", "Individual · LAP", 2240000m, "Loan against property", "13.75%", "LAP-STD-2026", "Branch walk-in", "In progress", "36 mo", new DateTime(2026, 7, 4, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004831", "S. Deshpande", "Jalgaon", new DateTime(2026, 7, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, "Shree Roadlines", "Individual · CV", 3120000m, "Commercial vehicle", "14.25%", "CV-STD-2026", "DSA", "Sanctioned", "72 mo", new DateTime(2026, 7, 5, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004844", "R. Kulkarni", "Nashik West", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 7, "Anil Jadhav", "Individual · LAP", 1500000m, "Loan against property", "13.00%", "LAP-STD-2026", "Digital", "In progress", "36 mo", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004859", "A. Rao", "Aurangabad", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Iqbal Transport LLP", "Individual · CV", 2675000m, "Commercial vehicle", "14.50%", "CV-STD-2026", "DSA", "In progress", "72 mo", new DateTime(2026, 7, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004868", "S. Deshpande", "Pune Camp", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, "Sunita Deshmukh", "Individual · LAP", 4200000m, "Loan against property", "14.50%", "LAP-STD-2026", "Branch walk-in", "In progress", "36 mo", new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "LN-2026-004871", "R. Kulkarni", "Nashik West", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, "Ramesh Pawar", "Individual · CV", 1850000m, "Commercial vehicle", "13.25%", "CV-STD-2026", "DSA", "In progress", "48 mo", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parties_ApplicationId_PartyType",
                table: "Parties",
                columns: new[] { "ApplicationId", "PartyType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Parties");

            migrationBuilder.DropTable(
                name: "Applications");
        }
    }
}

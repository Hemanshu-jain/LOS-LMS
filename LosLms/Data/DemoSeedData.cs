using LosLms.Models;
using LosLms.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace LosLms.Data;

/// <summary>
/// Fifteen loan applications with their stage data actually filled in, spread deliberately across
/// the lifecycle so every screen has something real to open.
/// </summary>
/// <remarks>
/// This replaces the 128 rows that used to arrive through <c>HasData</c>. Those had a customer name,
/// an amount and a stage number and nothing else — open any of them past the dashboard and every
/// screen was empty, which made the app impossible to demonstrate or test end to end.
///
/// A runtime seeder rather than <c>HasData</c>, for the same reason the users are:
/// <list type="bullet">
/// <item>child rows across twenty-odd tables would each need a hand-assigned primary key, and every
/// edit would rewrite the migration</item>
/// <item>queue dates are relative to today, so the dashboard's ageing and SLA colours stay truthful
/// instead of drifting into "56 days overdue" a month after the anchor was set</item>
/// <item>officer foreign keys point at Identity users, which do not exist at migration time</item>
/// </list>
///
/// Each application is populated up to the stage it has reached and no further, exactly as if an
/// officer had walked it there. A stage-6 file has RCU outcomes; it has no sanction, because it has
/// not been sanctioned.
///
/// Runs only when the Applications table is empty, and can be turned off outright with
/// <c>Seed:DemoApplications=false</c>.
/// </remarks>
public static class DemoSeedData
{
    private const decimal GstRate = 0.18m;
    private const decimal ProcessingFeeRate = 0.015m;

    /// <summary>Loan-to-value the demo files are written to, which fixes the margin and on-road cost.</summary>
    private const decimal TargetLtv = 0.80m;

    public static async Task SeedAsync(IServiceProvider services, ILogger logger, string contentRootPath)
    {
        using var scope = services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<LosDbContext>>();

        // Unscoped: startup has no signed-in user, and the tenant filter would otherwise hide the
        // rows this is checking for and reject the ones it creates.
        await using var db = new LosDbContext(options, TenantContext.ForSeeding());

        if (await db.Applications.AnyAsync())
        {
            return;
        }

        var officerIds = await db.Users
            .Where(u => u.CompanyId == LosDbContext.SeedCompanyId)
            .ToDictionaryAsync(u => u.DisplayName, u => u.Id);

        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var spec in Specs)
        {
            Build(db, spec, today, officerIds, contentRootPath);
        }

        await db.SaveChangesAsync();

        logger.LogInformation(
            "Demo data: seeded {Count} applications ({Sanctioned} sanctioned, {Progress} in progress, " +
            "{New} new, {Rejected} rejected).",
            Specs.Length,
            Specs.Count(s => s.Status == "Sanctioned"),
            Specs.Count(s => s.Status == "In progress"),
            Specs.Count(s => s.Status == "New"),
            Specs.Count(s => s.Status == "Rejected"));
    }

    // -------------------------------------------------------------------------------------------
    // The fifteen
    // -------------------------------------------------------------------------------------------

    /// <param name="Stage">1-8. Determines exactly how much child data exists.</param>
    /// <param name="QueuedDaysAgo">Drives the dashboard's ageing column and SLA colour.</param>
    /// <param name="CoApplicant">Null when the file has no co-applicant.</param>
    /// <param name="Guarantor">Null when the file has no guarantor.</param>
    private sealed record Spec(
        string Id,
        int Stage,
        string Status,
        string Product,
        string Branch,
        string Channel,
        string Officer,
        string Customer,
        decimal Amount,
        int Tenure,
        decimal Roi,
        int QueuedDaysAgo,
        string? CoApplicant,
        string? Guarantor,
        string Asset,
        string City,
        string Pin,
        decimal MonthlyIncome,
        bool Disbursed = false,
        bool RcuOverride = false,
        string? RejectReason = null);

    private const string Cv = "Commercial vehicle";
    private const string Lap = "Loan against property";

    private static readonly Spec[] Specs =
    {
        // ---- Stage 8: through the whole lifecycle ----
        new("LN-2026-005001", 8, "Sanctioned", Cv, "Nashik West", "DSA — Patil Motors", "R. Kulkarni",
            "Ramesh Bhaskar Pawar", 1_850_000m, 48, 13.25m, 34, "Sunita Ramesh Pawar", "Dattatray Pawar",
            "Tata Signa 2823.K tipper", "Nashik", "422003", 118_000m, Disbursed: true),

        new("LN-2026-005002", 8, "Sanctioned", Cv, "Pune Camp", "Branch walk-in", "S. Deshpande",
            "Sunita Anil Deshmukh", 2_400_000m, 60, 12.75m, 27, null, "Anil Deshmukh",
            "Ashok Leyland 3520 haulage", "Pune", "411001", 152_000m),

        // ---- Stage 7: sanctioned, and two still waiting on a decision ----
        new("LN-2026-005003", 7, "Sanctioned", Lap, "Nashik East", "DSA — Shree Associates", "A. Rao",
            "Mahesh Vitthal Jadhav", 4_200_000m, 84, 11.50m, 21, "Vaishali Jadhav", null,
            "Residential flat, Gangapur Road, Nashik", "Nashik", "422013", 240_000m),

        new("LN-2026-005004", 7, "In progress", Cv, "Aurangabad", "DSA — Patil Motors", "R. Kulkarni",
            "Kiran Madhukar Shinde", 1_575_000m, 48, 13.75m, 16, null, "Madhukar Shinde",
            "BharatBenz 1917R", "Aurangabad", "431001", 96_000m),

        new("LN-2026-005005", 7, "In progress", Lap, "Pune Camp", "Digital", "S. Deshpande",
            "Anjali Prashant Kulkarni", 3_500_000m, 72, 11.90m, 12, "Prashant Kulkarni", null,
            "Commercial shop, Camp, Pune", "Pune", "411002", 205_000m),

        // ---- Stage 6: eligibility computed, not yet at approval ----
        new("LN-2026-005006", 6, "In progress", Cv, "Nashik West", "DSA — Shree Associates", "A. Rao",
            "Vijay Namdev Sonawane", 2_140_000m, 54, 13.00m, 19, null, "Namdev Sonawane",
            "Tata Ultra 1918.T", "Nashik", "422009", 134_000m),

        new("LN-2026-005007", 6, "In progress", Cv, "Jalgaon", "Branch walk-in", "R. Kulkarni",
            "Prakash Shantaram Bhosale", 2_890_000m, 60, 14.25m, 31, "Manisha Bhosale", null,
            "Eicher Pro 6028 tanker", "Jalgaon", "425001", 149_000m),

        // ---- Stage 5: RCU back, one clean and one overridden ----
        new("LN-2026-005008", 5, "In progress", Lap, "Nashik East", "Digital", "S. Deshpande",
            "Sneha Rajendra Patil", 3_100_000m, 84, 11.75m, 9, null, null,
            "Residential row house, Indira Nagar, Nashik", "Nashik", "422009", 178_000m),

        new("LN-2026-005009", 5, "In progress", Cv, "Aurangabad", "DSA — Patil Motors", "A. Rao",
            "Ganesh Uttam Wagh", 1_260_000m, 42, 14.50m, 24, null, "Uttam Wagh",
            "Mahindra Furio 7", "Aurangabad", "431005", 82_000m, RcuOverride: true),

        // ---- Stage 4 down to 2: mid-file ----
        new("LN-2026-005010", 4, "In progress", Cv, "Pune Camp", "DSA — Shree Associates", "R. Kulkarni",
            "Nilesh Ashok Chavan", 1_980_000m, 48, 13.40m, 7, "Ashwini Chavan", null,
            "Tata Intra V50", "Pune", "411014", 121_000m),

        new("LN-2026-005011", 3, "In progress", Lap, "Nashik West", "Branch walk-in", "S. Deshpande",
            "Rohini Sadanand Salunke", 2_650_000m, 72, 12.10m, 5, null, null,
            "Residential plot, Pathardi Phata, Nashik", "Nashik", "422010", 143_000m),

        new("LN-2026-005012", 2, "In progress", Cv, "Jalgaon", "Digital", "A. Rao",
            "Amit Dattatray Gaikwad", 1_620_000m, 48, 13.60m, 3, null, null,
            "Ashok Leyland Dost+", "Jalgaon", "425002", 94_000m),

        // ---- Stage 1: barely started, and untouched ----
        new("LN-2026-005013", 1, "New", Cv, "Nashik East", "Branch walk-in", "R. Kulkarni",
            "Deepak Sopan More", 1_400_000m, 48, 13.50m, 2, null, null,
            "Tata Ace Gold", "Nashik", "422011", 78_000m),

        new("LN-2026-005014", 1, "New", Cv, "Nashik West", "Digital", "",
            "", 0m, 0, 0m, 1, null, null, "", "", "", 0m),

        // ---- Rejected mid-file ----
        new("LN-2026-005015", 5, "Rejected", Cv, "Aurangabad", "DSA — Patil Motors", "R. Kulkarni",
            "Santosh Balasaheb Kale", 2_200_000m, 60, 14.00m, 40, null, null,
            "Eicher Pro 2110", "Aurangabad", "431003", 71_000m,
            RejectReason: "RCU field verification negative on residence and business address; "
                          + "applicant untraceable at both. Deviation not supportable."),
    };

    // -------------------------------------------------------------------------------------------
    // Build
    // -------------------------------------------------------------------------------------------

    private static void Build(
        LosDbContext db,
        Spec spec,
        DateOnly today,
        IReadOnlyDictionary<string, string> officerIds,
        string contentRootPath)
    {
        var queued = today.AddDays(-spec.QueuedDaysAgo);
        var created = queued.ToDateTime(TimeOnly.MinValue);
        var isBlank = spec.Amount == 0m;

        var emi = MonthlyEmi(spec.Amount, spec.Roi, spec.Tenure);
        var processingFee = Math.Round(spec.Amount * ProcessingFeeRate, MidpointRounding.AwayFromZero);

        db.Applications.Add(new Application
        {
            Id = spec.Id,
            CompanyId = LosDbContext.SeedCompanyId,
            CustomerType = isBlank ? null : spec.Product == Cv ? "Individual · CV" : "Individual · LAP",

            // The untouched file has no branch either — the officer picks one on Loan & Security.
            Branch = isBlank ? null : spec.Branch,
            LoanProduct = isBlank ? null : spec.Product,
            Scheme = isBlank ? null : SchemeFor(spec),
            LoanAmount = spec.Amount,
            Tenure = isBlank ? null : spec.Tenure,
            Roi = isBlank ? null : spec.Roi,
            ProcessingFee = isBlank ? null : processingFee,
            AdvanceEmi = spec.Stage >= 2 && !isBlank ? emi : null,
            RepaymentMode = spec.Stage >= 2 && !isBlank ? "NACH" : null,
            DisbursalDate = spec.Disbursed ? queued.AddDays(spec.QueuedDaysAgo - 2) : null,
            CurrentStage = spec.Stage,
            Status = spec.Status,
            Disbursed = spec.Disbursed,
            CustomerName = isBlank ? null : spec.Customer,
            SourcingChannel = isBlank ? null : spec.Channel,
            AssignedOfficer = string.IsNullOrEmpty(spec.Officer) ? null : spec.Officer,
            AssignedOfficerId = officerIds.GetValueOrDefault(spec.Officer),
            CreatedAt = created,
            UpdatedAt = created,
        });

        if (isBlank)
        {
            // LN-2026-005014 stays genuinely empty: a file someone opened and walked away from.
            return;
        }

        AddParties(db, spec, created);

        if (spec.Stage >= 2) { AddLoanAndSecurity(db, spec, queued, emi); }
        if (spec.Stage >= 3) { AddBankAndFinancial(db, spec, queued); }
        if (spec.Stage >= 4) { AddDocuments(db, spec, queued, contentRootPath); }
        if (spec.Stage >= 5) { AddReportsRcu(db, spec, queued, officerIds, contentRootPath); }
        if (spec.Stage >= 6) { AddEligibility(db, spec, emi); }
        if (spec.Stage >= 7) { AddApprovals(db, spec, queued, officerIds, processingFee); }
        if (spec.Stage >= 8) { AddPostSanction(db, spec, queued, emi, contentRootPath); }

        if (spec.RejectReason is { } reason)
        {
            db.RejectionLogs.Add(new RejectionLog
            {
                ApplicationId = spec.Id,
                StageAtRejection = spec.Stage,
                Reason = reason,
                RejectedAt = created.AddDays(2),
            });
        }
    }

    // ---- Stage 1 -------------------------------------------------------------------------------

    private static void AddParties(LosDbContext db, Spec spec, DateTime created)
    {
        db.Parties.Add(NewParty(spec, "Applicant", spec.Customer, created, 0));

        if (spec.CoApplicant is { } coApplicant)
        {
            db.Parties.Add(NewParty(spec, "CoApplicant", coApplicant, created, 1));
        }

        if (spec.Guarantor is { } guarantor)
        {
            db.Parties.Add(NewParty(spec, "Guarantor", guarantor, created, 2));
        }
    }

    private static Party NewParty(Spec spec, string partyType, string fullName, DateTime created, int index)
    {
        var seed = Seed(spec.Id) + index;
        var isApplicant = partyType == "Applicant";

        return new Party
        {
            ApplicationId = spec.Id,
            PartyType = partyType,
            FullName = fullName,
            DateOfBirth = new DateOnly(1970 + seed % 18, 1 + seed % 12, 1 + seed % 27),
            Gender = fullName.Split(' ')[0] is "Sunita" or "Anjali" or "Sneha" or "Rohini" or "Vaishali"
                or "Manisha" or "Ashwini" ? "Female" : "Male",
            MaritalStatus = "Married",
            FatherSpouseName = partyType == "CoApplicant" ? spec.Customer : "Late " + fullName.Split(' ')[^1],
            CustomerCategory = "Individual",
            Nationality = "Indian",
            MotherTongue = "Marathi",
            Pan = Pan(fullName, seed),
            Aadhaar = Aadhaar(seed),
            Mobile = Mobile(seed),
            AltMobile = spec.Stage >= 3 ? Mobile(seed + 7) : null,
            Email = Email(fullName),
            Address1 = $"{100 + seed % 400}, {StreetFor(seed)}",
            Address2 = spec.City + " " + (spec.Stage >= 3 ? "West" : "East"),
            City = spec.City,
            State = "Maharashtra",
            PinCode = spec.Pin,
            ResidenceType = seed % 3 == 0 ? "Rented" : "Owned",
            YearsAtAddress = 3 + seed % 12,

            // Transport operators are self-employed; LAP files here are salaried or business owners.
            EmploymentType = spec.Product == Cv ? "Self-employed" : seed % 2 == 0 ? "Salaried" : "Business owner",
            EmployerName = spec.Product == Cv ? $"{fullName.Split(' ')[^1]} Roadlines" : "Sahyadri Industries Ltd",
            Designation = spec.Product == Cv ? "Proprietor" : "Senior Manager",
            OfficeAddress = $"Plot {10 + seed % 60}, MIDC, {spec.City}",

            // Only the applicant carries the income the eligibility maths uses.
            MonthlyIncome = isApplicant ? spec.MonthlyIncome
                : partyType == "CoApplicant" ? Math.Round(spec.MonthlyIncome * 0.35m) : 0m,
            YearsInJob = 4 + seed % 14,

            // Never fabricated: no verification provider is configured anywhere in this build, so a
            // seeded row must not claim a check that never happened.
            PanVerified = false,
            AadhaarVerified = false,
            MobileVerified = false,
            DedupeStatus = spec.Stage >= 2 ? "Pass" : "NotRun",

            CreatedAt = created,
            UpdatedAt = created,
        };
    }

    // ---- Stage 2 -------------------------------------------------------------------------------

    private static void AddLoanAndSecurity(LosDbContext db, Spec spec, DateOnly queued, decimal emi)
    {
        var isVehicle = spec.Product == Cv;
        var onRoad = OnRoadCost(spec.Amount);
        var seed = Seed(spec.Id);

        db.SecurityDetails.Add(new SecurityDetail
        {
            ApplicationId = spec.Id,
            AssetType = isVehicle ? "Vehicle" : "Property",

            MakeModel = isVehicle ? spec.Asset : null,
            MfgYear = isVehicle ? (2024 + seed % 2).ToString() : null,
            RegNo = isVehicle ? $"MH{15 + seed % 6:D2}{Letters(seed)}{1000 + seed % 8999}" : null,
            ChassisNo = isVehicle ? $"MAT{seed:D6}RN{seed % 100:D2}" : null,
            EngineNo = isVehicle ? $"ENG{seed:D7}" : null,
            InvoiceNo = isVehicle ? $"INV/{queued:yyyy}/{2000 + seed % 900}" : null,
            InvoiceDate = isVehicle ? queued.AddDays(-6) : null,
            InvoiceValue = isVehicle ? onRoad : null,
            InsuranceProvider = isVehicle ? "ICICI Lombard" : null,
            PolicyNo = isVehicle ? $"POL{seed:D8}" : null,
            PolicyExpiry = isVehicle ? queued.AddYears(1) : null,

            PropertyType = isVehicle ? null : PropertyTypeFor(spec.Asset),
            PropertyAddress = isVehicle ? null : spec.Asset,
            Area = isVehicle ? null : 850m + seed % 900,
            OwnershipType = isVehicle ? null : "Freehold",
            SaleDeedNo = isVehicle ? null : $"SD/{queued:yyyy}/{1200 + seed % 700}",
            ValuationRefNo = isVehicle ? null : $"VAL/{queued:yyyy}/{300 + seed % 400}",
            EncumbranceRef = isVehicle ? null : $"EC/{queued:yyyy}/{500 + seed % 400}",
            AssessedValue = isVehicle ? null : onRoad,

            CreatedAt = queued.ToDateTime(TimeOnly.MinValue),
            UpdatedAt = queued.ToDateTime(TimeOnly.MinValue),
        });

        // Two references, which is the screen's minimum. When there is a co-applicant, Loan & Security
        // derives the first row from them at render time — so only the second is stored here, and the
        // derived one is rebuilt from Parties on every load.
        if (spec.CoApplicant is null)
        {
            db.References.Add(NewReference(spec, seed, 0, "Business associate"));
        }

        db.References.Add(NewReference(spec, seed, 1, "Neighbour"));

        var household = Math.Round(spec.MonthlyIncome * 0.28m, MidpointRounding.AwayFromZero);
        var fuelDriver = spec.Product == Cv
            ? Math.Round(spec.MonthlyIncome * 0.22m, MidpointRounding.AwayFromZero)
            : 0m;

        db.Viabilities.Add(new Viability
        {
            ApplicationId = spec.Id,
            IncomeFreight = spec.Product == Cv ? Math.Round(spec.MonthlyIncome * 0.75m) : 0m,
            IncomeSalary = spec.Product == Cv ? 0m : Math.Round(spec.MonthlyIncome * 0.80m),
            IncomeOther = Math.Round(spec.MonthlyIncome * 0.20m),
            ExpenseHousehold = household,
            ExpenseFuelDriver = fuelDriver,
            ExpenseExistingEmi = spec.Stage >= 6 ? Math.Round(emi * 0.30m, MidpointRounding.AwayFromZero) : 0m,
            CreatedAt = queued.ToDateTime(TimeOnly.MinValue),
            UpdatedAt = queued.ToDateTime(TimeOnly.MinValue),
        });
    }

    private static Reference NewReference(Spec spec, int seed, int index, string relationship) => new()
    {
        ApplicationId = spec.Id,
        Name = ReferenceNames[(seed + index) % ReferenceNames.Length],
        Relationship = relationship,
        Mobile = Mobile(seed + 20 + index),
        Address = $"{20 + (seed + index) % 60}, {StreetFor(seed + index)}, {spec.City}",
        KnownSince = $"{4 + (seed + index) % 11} years",
        CreatedAt = spec.QueuedDaysAgo > 0 ? DateTime.UtcNow : DateTime.UtcNow,
    };

    // ---- Stage 3 -------------------------------------------------------------------------------

    private static void AddBankAndFinancial(LosDbContext db, Spec spec, DateOnly queued)
    {
        var seed = Seed(spec.Id);
        var bank = Banks[seed % Banks.Length];
        var stamped = queued.ToDateTime(TimeOnly.MinValue);

        db.BankDetails.Add(new BankDetail
        {
            ApplicationId = spec.Id,
            BankName = bank.Name,
            AccountNumber = $"{bank.Prefix}{seed:D6}{1000 + seed % 8999}",
            Ifsc = $"{bank.Ifsc}0{100000 + seed % 899999}",
            AccountType = spec.Product == Cv ? "Current" : "Savings",
            AccountHolderName = spec.Customer,

            // Must be one of the screen's own options or the select renders blank and the next save
            // writes the blank back. Note the EN DASH — Bank & Financial and Approvals spell their
            // vintage ranges differently, so the two are not interchangeable.
            Vintage = BankVintages[seed % BankVintages.Length],

            // No banking verification provider is configured, so this stays unrun. Nothing here may
            // claim a name match that never took place.
            PennyDropStatus = "NotRun",

            CreatedAt = stamped,
            UpdatedAt = stamped,
        });

        // Six months of statements, which is what the screen expects to see.
        for (var i = 0; i < 6; i++)
        {
            var month = queued.AddMonths(-(i + 1));
            db.BankStatements.Add(new BankStatement
            {
                ApplicationId = spec.Id,
                Period = month.ToString("MMM yyyy"),
                UploadedOn = stamped,

                // No statement parser is configured — the screen shows this as "Not configured".
                ParsedStatus = "NotConfigured",
                CreatedAt = stamped,
            });
        }

        var onRoad = OnRoadCost(spec.Amount);
        var exShowroom = Math.Round(onRoad * 0.82m, MidpointRounding.AwayFromZero);
        var body = Math.Round(onRoad * 0.12m, MidpointRounding.AwayFromZero);
        var insurance = onRoad - exShowroom - body;
        var margin = onRoad - spec.Amount;

        db.CamCostBreakdowns.Add(new CamCostBreakdown
        {
            ApplicationId = spec.Id,
            DraftExShowroomCost = exShowroom,
            DraftBodyAccessories = body,
            DraftInsuranceRegistration = insurance,
            DraftMargin = margin,
            AppliedExShowroomCost = exShowroom,
            AppliedBodyAccessories = body,
            AppliedInsuranceRegistration = insurance,
            AppliedMargin = margin,
            LastRecalculatedAt = stamped,
            CreatedAt = stamped,
            UpdatedAt = stamped,
        });
    }

    // ---- Stage 4 -------------------------------------------------------------------------------

    /// <summary>
    /// Marks the KYC set as collected for every party on the file, with a real file behind each one
    /// so the preview and download actually work. The Document Checklist screen creates the rows for
    /// the types not listed here when it first opens, so only what is genuinely collected is seeded.
    /// </summary>
    private static void AddDocuments(LosDbContext db, Spec spec, DateOnly queued, string contentRootPath)
    {
        var stamped = queued.ToDateTime(TimeOnly.MinValue);
        var parties = new List<string> { "Applicant" };
        if (spec.CoApplicant is not null) { parties.Add("CoApplicant"); }
        if (spec.Guarantor is not null) { parties.Add("Guarantor"); }

        foreach (var partyType in parties)
        {
            foreach (var (type, label) in CollectedDocuments)
            {
                var fileName = $"{type.ToLowerInvariant()}-{Guid.NewGuid():N}.pdf";
                var relativePath = WriteDemoPdf(
                    contentRootPath, spec.Id, partyType, fileName,
                    label, $"{spec.Id} · {partyType}");

                db.ChecklistDocuments.Add(new ChecklistDocument
                {
                    ApplicationId = spec.Id,
                    PartyType = partyType,
                    DocumentType = type,
                    FilePath = relativePath,
                    UploadDate = stamped,

                    // Address proof is the only type with a shelf life on this screen.
                    TargetDate = type == "Address" ? queued.AddDays(90) : null,
                    ValidityDays = type == "Address" ? 90 : null,
                    CreatedAt = stamped,
                    UpdatedAt = stamped,
                });
            }
        }
    }

    // ---- Stage 5 -------------------------------------------------------------------------------

    private static void AddReportsRcu(
        LosDbContext db,
        Spec spec,
        DateOnly queued,
        IReadOnlyDictionary<string, string> officerIds,
        string contentRootPath)
    {
        var seed = Seed(spec.Id);
        var stamped = queued.ToDateTime(TimeOnly.MinValue);
        var initiated = queued.AddDays(2);
        var completed = initiated.AddDays(3);
        var officerId = officerIds.GetValueOrDefault(spec.Officer);

        db.RcuInitiations.Add(new RcuInitiation
        {
            ApplicationId = spec.Id,
            Mode = seed % 3 == 0 ? "Sampled" : "Screened",
            Branch = spec.Branch,
            Vendor = RcuVendors[seed % RcuVendors.Length],
            InitiationDate = initiated,
            CompletionDate = completed,
            Tat = completed.DayNumber - initiated.DayNumber,
            CaseRef = $"RCU-{queued:yyyy}-{10000 + seed % 800:D5}",

            // The overridden file is the one whose outcomes came back negative. All three override
            // fields are set together, which is what the screen's gate requires.
            OverrideActive = spec.RcuOverride,
            OverrideReason = spec.RcuOverride
                ? "Address discrepancy explained by a recent shift; fresh utility bill and landlord "
                  + "confirmation collected. Approved as a one-off deviation."
                : null,
            OverrideApproverOfficerId = spec.RcuOverride ? officerIds.GetValueOrDefault("R. Kulkarni") : null,

            CreatedAt = stamped,
            UpdatedAt = stamped,
        });

        // Applicant and Co-Applicant rows are ALWAYS rendered by the RCU screen; only Guarantor is
        // conditional on the party existing. An outcome is seeded for every row the screen will show,
        // because the overall status is derived from all of them and a missing row reads as Pending —
        // which would leave a fully disbursed file showing an unfinished RCU.
        var parties = new List<string> { "Applicant", "CoApplicant" };
        if (spec.Guarantor is not null) { parties.Add("Guarantor"); }

        foreach (var partyType in parties)
        {
            // A rejected or overridden file is one where the field verification came back negative.
            var negative = (spec.RcuOverride || spec.RejectReason is not null) && partyType == "Applicant";
            var noSuchParty = partyType == "CoApplicant" && spec.CoApplicant is null;

            db.RcuOutcomes.Add(new RcuOutcome
            {
                ApplicationId = spec.Id,
                PartyType = partyType,
                Status = negative ? "NotRecommended" : "Recommended",
                VerifiedOn = noSuchParty ? null : completed,
                VerifiedByOfficerId = noSuchParty ? null : officerId,
                Remarks = noSuchParty
                    ? "Not applicable — this file has no co-applicant."
                    : negative
                        ? "Residence not confirmed at the declared address; neighbours report the "
                          + "family moved out. Business premises locked on both visits."
                        : "Residence and business premises confirmed. Neighbour and staff references "
                          + "consistent.",
                CreatedAt = stamped,
                UpdatedAt = stamped,
            });
        }

        var reportFile = $"rcu-report-{Guid.NewGuid():N}.pdf";
        db.RcuReports.Add(new RcuReport
        {
            ApplicationId = spec.Id,
            SequenceNumber = 1,
            FilePath = WriteDemoPdf(contentRootPath, spec.Id, "rcu", reportFile,
                "RCU field verification report", $"{spec.Id} · {spec.Customer}"),
            UploadedAt = stamped,
            Note = "Field verification report — residence and business address.",
            CreatedAt = stamped,
        });
    }

    // ---- Stage 6 -------------------------------------------------------------------------------

    private static void AddEligibility(LosDbContext db, Spec spec, decimal emi)
    {
        var seed = Seed(spec.Id);
        var stamped = DateTime.UtcNow;

        db.Classifications.Add(new Classification
        {
            ApplicationId = spec.Id,

            // Road transport operators are a priority sector; the LAP files here are not.
            Psl = spec.Product == Cv ? "Yes - Priority Sector" : "No - Non-Priority Sector",
            PslSub = spec.Product == Cv ? "Transport Sector - CV" : "Other",
            RiskSharing = 0m,
            CoLendingPartner = "None",
            EndUse = spec.Product == Cv
                ? seed % 2 == 0 ? "Fleet expansion" : "Vehicle replacement"
                : "Working capital",
            PrioritySectorAmount = spec.Product == Cv ? spec.Amount : null,
            CreatedAt = stamped,
            UpdatedAt = stamped,
        });

        // One running loan, so the FOIR maths has something real to work against.
        db.ExistingLoans.Add(new ExistingLoan
        {
            ApplicationId = spec.Id,
            PartyType = "Applicant",
            Lender = Banks[(seed + 2) % Banks.Length].Name,
            LoanType = spec.Product == Cv ? "Commercial vehicle" : "Business loan",
            Sanctioned = Math.Round(spec.Amount * 0.45m, MidpointRounding.AwayFromZero),
            Pos = Math.Round(spec.Amount * 0.18m, MidpointRounding.AwayFromZero),
            Emi = Math.Round(emi * 0.30m, MidpointRounding.AwayFromZero),
            Roi = 13.00m,
            MaxDpd = seed % 4 == 0 ? 12 : 0,
            Bounces = seed % 4 == 0 ? 1 : 0,
            Rtr = seed % 4 == 0 ? "Irregular" : "Regular",
            CreatedAt = stamped,
            UpdatedAt = stamped,
        });

        db.BankingRecords.Add(new BankingRecord
        {
            ApplicationId = spec.Id,
            PartyType = "Applicant",
            Bank = Banks[seed % Banks.Length].Name,
            Months = 6,
            AvgBalance = Math.Round(spec.MonthlyIncome * 0.85m, MidpointRounding.AwayFromZero),
            Bounces = seed % 5 == 0 ? 2 : 0,
            InwardPct = 62m + seed % 20,
            OutwardPct = 38m - seed % 12,
            CreatedAt = stamped,
            UpdatedAt = stamped,
        });

        db.EligibilityDecisions.Add(new EligibilityDecision
        {
            ApplicationId = spec.Id,
            ApproverNote = spec.Stage >= 7
                ? "Cash flows verified against six months of banking and trip records. Existing "
                  + "obligation is regular. Recommended at the sanctioned terms."
                : null,
            CreatedAt = stamped,
            UpdatedAt = stamped,
        });
    }

    // ---- Stage 7 -------------------------------------------------------------------------------

    private static void AddApprovals(
        LosDbContext db,
        Spec spec,
        DateOnly queued,
        IReadOnlyDictionary<string, string> officerIds,
        decimal processingFee)
    {
        var seed = Seed(spec.Id);
        var stamped = DateTime.UtcNow;
        var sanctioned = spec.Status == "Sanctioned";
        var surname = spec.Customer.Split(' ')[^1];

        db.Businesses.Add(new Business
        {
            ApplicationId = spec.Id,
            FirmName = spec.Product == Cv ? $"{surname} Roadlines" : $"{surname} Enterprises",
            Constitution = "Proprietorship",
            Gstin = $"27{Pan(spec.Customer, seed)}1Z{seed % 10}",

            // Approvals spells its ranges with a plain hyphen, unlike Bank & Financial's en dash.
            Vintage = seed % 2 == 0 ? "5-10 years" : "10+ years",
            IncorpDate = new DateOnly(2012 + seed % 9, 1 + seed % 12, 1 + seed % 27),
            Turnover = Math.Round(spec.MonthlyIncome * 12m * 2.4m, MidpointRounding.AwayFromZero),
            Narrative = spec.Product == Cv
                ? "Operates a small fleet on the Nashik–Mumbai and Nashik–Pune corridors, hauling "
                  + "agricultural produce and packaged goods for two regular counterparties on "
                  + "monthly credit terms. Seasonality is mild, peaking around the rabi harvest."
                : "Trades in industrial fasteners and fabrication consumables, supplying MIDC units "
                  + "on 45-day credit. Property offered is self-occupied and free of encumbrance.",
            CreatedAt = stamped,
            UpdatedAt = stamped,
        });

        db.Partners.Add(new Partner
        {
            ApplicationId = spec.Id,
            Name = spec.Customer,
            Role = "Proprietor",
            Pan = Pan(spec.Customer, seed),
            Contact = Mobile(seed),
            Shareholding = 100m,
            Dob = new DateOnly(1970 + seed % 18, 1 + seed % 12, 1 + seed % 27),
            CreatedAt = stamped,
            UpdatedAt = stamped,
        });

        db.Tvrs.Add(new Tvr
        {
            ApplicationId = spec.Id,
            Agent = spec.Officer,
            PersonContacted = spec.Customer,
            Relationship = "Self",
            Status = "Positive - Confirmed",
            RecordingRef = $"TVR-{queued:yyyy}-{4000 + seed % 900}",
            CallDateTime = queued.AddDays(4).ToDateTime(new TimeOnly(11, 20)),
            Summary = "Spoke with the applicant for 15 minutes. Employment, business address and "
                      + "monthly turnover confirmed and consistent with the file.",
            CreatedAt = stamped,
            UpdatedAt = stamped,
        });

        // The charge set the Approvals screen would otherwise create on first open, with the same
        // heads and the same locked processing fee.
        db.Charges.AddRange(
            NewCharge(spec.Id, "Processing fee", "Pulled from Loan & Security", processingFee,
                Math.Round(processingFee * GstRate, MidpointRounding.AwayFromZero), locked: true),
            NewCharge(spec.Id, "Documentation charge", "Flat", 2500m, 450m),
            NewCharge(spec.Id, "Stamp duty", "As per state rate", 5000m, 0m),
            NewCharge(spec.Id, "Valuation fee", "Flat", 3000m, 540m));

        // 005005 is deliberately half-signed: the recommender has signed, the approver has not.
        var recommenderOnly = spec.Id == "LN-2026-005005";

        db.ApprovalDecisions.Add(new ApprovalDecision
        {
            ApplicationId = spec.Id,
            ApprovalNote = "Cash flows support the proposed EMI with headroom. Security is adequate "
                           + "and the LTV is within scheme limits. RCU and TVR are positive. "
                           + "Recommended for sanction at the terms below.",
            RecommenderUserId = officerIds.GetValueOrDefault("S. Deshpande"),
            RecommenderRole = "Senior Credit Analyst",
            RecommenderDate = queued.AddDays(6),
            ApproverUserId = recommenderOnly ? null : officerIds.GetValueOrDefault("R. Kulkarni"),
            ApproverRole = recommenderOnly ? null : "Branch Credit Manager",
            ApproverDate = recommenderOnly ? null : queued.AddDays(7),
            Authority = recommenderOnly ? null : "Branch Credit Manager",
            SanctionedAmount = spec.Amount,
            SanctionedRoi = spec.Roi,
            SanctionedTenure = spec.Tenure,
            Conditions = "1. Hypothecation to be endorsed on the RC within 30 days of disbursement.\n"
                         + "2. Comprehensive insurance with the lender as loss payee, renewed annually.\n"
                         + "3. NACH mandate to be registered before the first EMI falls due.",

            // Only a genuinely sanctioned file carries the confirmation; the two still at approval
            // must show the sanction button blocked, which is the point of having them.
            SanctionConfirmed = sanctioned,
            SanctionConfirmedAt = sanctioned ? stamped : null,

            CreatedAt = stamped,
            UpdatedAt = stamped,
        });
    }

    private static Charge NewCharge(
        string applicationId, string head, string basis, decimal amount, decimal gst, bool locked = false) => new()
    {
        ApplicationId = applicationId,
        Head = head,
        Basis = basis,
        Amount = amount,
        Gst = gst,
        DeductedFrom = "Disbursement",
        Locked = locked,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    // ---- Stage 8 -------------------------------------------------------------------------------

    private static void AddPostSanction(
        LosDbContext db, Spec spec, DateOnly queued, decimal emi, string contentRootPath)
    {
        var seed = Seed(spec.Id);
        var stamped = DateTime.UtcNow;
        var bank = Banks[seed % Banks.Length];
        var account = $"{bank.Prefix}{seed:D6}{1000 + seed % 8999}";
        var valueDate = queued.AddDays(spec.QueuedDaysAgo - 2);

        db.Disbursements.Add(new Disbursement
        {
            ApplicationId = spec.Id,
            Type = spec.Product == Cv ? "Direct to dealer" : "Direct to customer",
            BeneficiaryName = spec.Product == Cv ? "Patil Motors Pvt Ltd" : spec.Customer,
            BeneficiaryAccount = $"{bank.Prefix}{seed + 31:D6}{2000 + seed % 7999}",
            PaymentMode = "NEFT",
            ValueDate = spec.Disbursed ? valueDate : null,
            Utr = spec.Disbursed ? $"UTR{queued:yyyyMMdd}{100000 + seed % 899999}" : null,
            FirstEmiDate = spec.Disbursed ? valueDate.AddMonths(1) : null,
            MemoFilePath = spec.Disbursed
                ? WriteDemoPdf(contentRootPath, spec.Id, "postsanction",
                    $"memo-{Guid.NewGuid():N}.pdf", "Disbursement memo", $"{spec.Id} · {spec.Customer}")
                : null,
            CreatedAt = stamped,
            UpdatedAt = stamped,
        });

        // Registered only where the money has actually moved, and only ever by the physical route —
        // no digital mandate provider is configured, so nothing may claim a digital registration.
        var registered = spec.Disbursed;

        db.EnachMandates.Add(new EnachMandate
        {
            ApplicationId = spec.Id,
            Umrn = registered ? $"HDFC{queued:yyyyMM}{seed % 100000:D5}" : null,
            Status = registered ? "Registered" : "Pending",
            DebitDate = registered ? valueDate.AddMonths(1) : null,
            LinkedAccount = $"{bank.Name} · {account}",
            AccountNumber = account,
            Ifsc = $"{bank.Ifsc}0{100000 + seed % 899999}",
            BankName = bank.Name,
            MandateType = registered ? "Physical" : null,
            ConfirmationAccepted = registered,
            NameMatchStatus = "NotRun",
            CreatedAt = stamped,
            UpdatedAt = stamped,
        });

        db.SecurityNachMandates.Add(new SecurityNachMandate
        {
            ApplicationId = spec.Id,
            Status = "Pending",
            MandateHolder = spec.Guarantor,
            NameMatchStatus = "NotRun",
            CreatedAt = stamped,
            UpdatedAt = stamped,
        });

        var downPayment = OnRoadCost(spec.Amount) - spec.Amount;

        db.DownPaymentRecords.Add(new DownPaymentRecord
        {
            ApplicationId = spec.Id,
            AmountReceived = spec.Disbursed ? downPayment : 0m,
            ReceiptNo = spec.Disbursed ? $"DP/{queued:yyyy}/{700 + seed % 200}" : null,
            ReceivedDate = spec.Disbursed ? valueDate.AddDays(-1) : null,
            CreatedAt = stamped,
            UpdatedAt = stamped,
        });

        // Release funds is gated on every checklist item being cleared, so the disbursed file has all
        // seven cleared and the one still awaiting release does not.
        var checklist = new (string Item, string Owner)[]
        {
            ("CAM approval", "Credit head"),
            ("Rate & PF approval", "Business head"),
            ("Agreement executed", "Branch ops"),
            ("RC / hypothecation endorsement", "Branch ops"),
            ("Insurance assignment", "Branch ops"),
            ("E-Nach registration", "Branch ops"),
            ("Down payment receipt", "Cashier"),
        };

        for (var i = 0; i < checklist.Length; i++)
        {
            // The undisbursed file has the first three cleared and the rest outstanding — a realistic
            // half-done state, and the reason its Release funds button stays blocked.
            var cleared = spec.Disbursed || i < 3;

            db.PostSanctionChecklists.Add(new PostSanctionChecklist
            {
                ApplicationId = spec.Id,
                Item = checklist[i].Item,
                Owner = checklist[i].Owner,
                Flag = cleared ? "Cleared" : "Pending",
                ClearedOn = cleared ? valueDate.AddDays(-2) : null,
                Remarks = cleared ? "Verified and filed." : null,
                CreatedAt = stamped,
                UpdatedAt = stamped,
            });
        }

        var pdds = new (string Item, string Responsible)[]
        {
            ("Registration certificate", "Branch ops"),
            ("Invoice — original", "Branch ops"),
            ("Guarantor re-verification", "Credit"),
            ("Insurance endorsement copy", "Branch ops"),
        };

        for (var i = 0; i < pdds.Length; i++)
        {
            db.Pdds.Add(new Pdd
            {
                ApplicationId = spec.Id,
                Item = pdds[i].Item,
                Responsible = pdds[i].Responsible,
                ExpectedDate = valueDate.AddDays(30 + i * 15),

                // Post-disbursement documents are genuinely still outstanding on a fresh file; the
                // originals come back from the dealer and the RTO over the following weeks.
                Status = spec.Disbursed && i == 0 ? "Received" : "Open",
                CreatedAt = stamped,
                UpdatedAt = stamped,
            });
        }
    }

    // -------------------------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------------------------

    /// <summary>Standard reducing-balance EMI. Zero rate or tenure degrades to a plain division.</summary>
    private static decimal MonthlyEmi(decimal principal, decimal annualRatePct, int months)
    {
        if (principal <= 0m || months <= 0) { return 0m; }
        if (annualRatePct <= 0m) { return Math.Round(principal / months, MidpointRounding.AwayFromZero); }

        var r = (double)annualRatePct / 12d / 100d;
        var growth = Math.Pow(1 + r, months);
        var emi = (double)principal * r * growth / (growth - 1);

        return Math.Round((decimal)emi, MidpointRounding.AwayFromZero);
    }

    /// <summary>On-road cost implied by the target LTV, which is what makes the margin add up.</summary>
    private static decimal OnRoadCost(decimal loanAmount) =>
        Math.Round(loanAmount / TargetLtv, MidpointRounding.AwayFromZero);

    private static string SchemeFor(Spec spec) =>
        spec.Product == Lap ? "LAP-STD-2026"
        : spec.Amount >= 2_000_000m ? "CV-PRIME-2026"
        : "CV-STD-2026";

    /// <summary>Stable per-application number, so a rebuild produces identical demo data.</summary>
    private static int Seed(string applicationId) =>
        int.TryParse(applicationId.AsSpan(applicationId.LastIndexOf('-') + 1), out var n) ? n : 1;

    private static string Pan(string name, int seed)
    {
        var initials = new string(name.Split(' ').Where(p => p.Length > 0)
            .Select(p => char.ToUpperInvariant(p[0])).Take(3).ToArray()).PadRight(3, 'X');

        return $"{initials}PK{seed % 10000:D4}{(char)('A' + seed % 26)}";
    }

    private static string Aadhaar(int seed) => $"{2000 + seed % 7999:D4}{seed % 9999:D4}{seed * 7 % 9999:D4}";

    private static string Mobile(int seed) => $"9{800000000 + seed * 137 % 99999999:D9}"[..10];

    private static string Email(string name) =>
        name.Split(' ')[0].ToLowerInvariant() + "." + name.Split(' ')[^1].ToLowerInvariant() + "@example.in";

    private static string Letters(int seed) =>
        $"{(char)('A' + seed % 26)}{(char)('A' + (seed / 3) % 26)}";

    private static string StreetFor(int seed) => Streets[seed % Streets.Length];

    private static readonly string[] Streets =
    {
        "Tilak Road", "Gangapur Road", "Shivaji Nagar", "College Road", "MG Road",
        "Canada Corner", "Indira Nagar", "Sharanpur Road",
    };

    private static readonly string[] ReferenceNames =
    {
        "Sanjay Bhoir", "Meena Kulkarni", "Pravin Tambe", "Asha Jagtap",
        "Vilas Nikam", "Sujata Bagul", "Rajesh Ahire", "Nanda Wagh",
    };

    /// <summary>
    /// Every value the seeder writes into a field the UI renders as a select has to be one of that
    /// select's own options. Anything else shows blank and, worse, the next save writes the blank
    /// back over it. Bank &amp; Financial uses an EN DASH in its ranges; Approvals uses a hyphen.
    /// </summary>
    private static readonly string[] BankVintages = { "1–3 years", "3–5 years", "5+ years" };

    private static string PropertyTypeFor(string asset) =>
        asset.StartsWith("Commercial", StringComparison.OrdinalIgnoreCase) ? "Commercial"
        : asset.Contains("plot", StringComparison.OrdinalIgnoreCase) ? "Land"
        : "Residential";

    private static readonly (string Name, string Ifsc, string Prefix)[] Banks =
    {
        ("HDFC Bank", "HDFC", "50100"),
        ("State Bank of India", "SBIN", "38210"),
        ("ICICI Bank", "ICIC", "62450"),
        ("Bank of Baroda", "BARB", "29110"),
        ("Axis Bank", "UTIB", "91800"),
    };

    private static readonly string[] RcuVendors =
    {
        "Verified Field Services", "TransUnion CIBIL RCU", "CRISIL Risk Solutions", "SecureCheck Verifications",
    };

    /// <summary>The KYC set treated as collected on a file that has reached the checklist stage.</summary>
    private static readonly (string Type, string Label)[] CollectedDocuments =
    {
        ("Pan", "PAN card"),
        ("Aadhaar", "Aadhaar (masked)"),
        ("Photo", "Photograph"),
        ("Signature", "Signature proof"),
        ("Address", "Address proof"),
    };

    /// <summary>
    /// Writes a small real PDF so the preview and download actually open something, and returns the
    /// stored relative path.
    /// </summary>
    /// <remarks>
    /// Generated with QuestPDF, which the project already uses for the CAM — a hand-rolled byte
    /// literal would be a malformed PDF that fails silently in the viewer.
    /// </remarks>
    private static string WriteDemoPdf(
        string contentRootPath, string applicationId, string folder, string fileName,
        string title, string subtitle)
    {
        var directory = Path.Combine(contentRootPath, "App_Data", "uploads", applicationId, folder);
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, fileName);

        if (!File.Exists(fullPath))
        {
            var bytes = Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(36);
                    page.DefaultTextStyle(t => t.FontSize(11).FontColor("#1a1f29"));

                    page.Content().Column(column =>
                    {
                        column.Item().Text(title).FontSize(16).Bold();
                        column.Item().PaddingTop(4).Text(subtitle).FontSize(10).FontColor("#5b6472");
                        column.Item().PaddingTop(24).Text(
                            "Placeholder document generated with the demo data set. It stands in for a "
                            + "scanned original so the preview, download and remarks can be exercised; "
                            + "it is not a real record and contains no real personal information.")
                            .FontSize(10).LineHeight(1.4f);
                    });
                });
            }).GeneratePdf();

            File.WriteAllBytes(fullPath, bytes);
        }

        return Path.Combine("App_Data", "uploads", applicationId, folder, fileName);
    }
}

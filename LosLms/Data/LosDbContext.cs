using LosLms.Models;
using LosLms.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LosLms.Data;

/// <summary>
/// Entity Framework Core context for the central LOS/LMS MySQL database, and the Identity store.
/// </summary>
/// <remarks>
/// PROVISIONAL SCHEMA — the two entities here exist to make the Applications Dashboard and
/// Customer Details screens functional. The full schema is designed once all nine screens exist.
///
/// TENANT ISOLATION IS STRUCTURAL. The three global query filters at the top of
/// <see cref="OnModelCreating"/> are the whole mechanism — no screen has to remember a
/// <c>Where(x =&gt; x.CompanyId == ...)</c>, and forgetting one cannot leak another company's data.
/// The tenant is snapshotted into plain fields at construction rather than read through a property
/// chain, because EF parameterises a captured context field cleanly and a chain is fragile.
/// </remarks>
public class LosDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly int? _companyId;
    private readonly bool _isSuperAdmin;
    private readonly bool _hasUser;

    public LosDbContext(DbContextOptions<LosDbContext> options, TenantContext tenant)
        : base(options)
    {
        _companyId = tenant.CompanyId;
        _isSuperAdmin = tenant.IsSuperAdmin;
        _hasUser = tenant.HasUser;
    }

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<VehicleLoanCap> VehicleLoanCaps => Set<VehicleLoanCap>();

    public DbSet<Application> Applications => Set<Application>();

    public DbSet<Party> Parties => Set<Party>();

    public DbSet<SecurityDetail> SecurityDetails => Set<SecurityDetail>();

    public DbSet<Reference> References => Set<Reference>();

    public DbSet<Viability> Viabilities => Set<Viability>();

    public DbSet<BankDetail> BankDetails => Set<BankDetail>();

    public DbSet<BankStatement> BankStatements => Set<BankStatement>();

    public DbSet<CamCostBreakdown> CamCostBreakdowns => Set<CamCostBreakdown>();

    public DbSet<IfscBankLookup> IfscBankLookups => Set<IfscBankLookup>();

    public DbSet<ChecklistDocument> ChecklistDocuments => Set<ChecklistDocument>();

    public DbSet<DocumentRemark> DocumentRemarks => Set<DocumentRemark>();

    public DbSet<RcuInitiation> RcuInitiations => Set<RcuInitiation>();

    public DbSet<RcuOutcome> RcuOutcomes => Set<RcuOutcome>();

    public DbSet<RcuReport> RcuReports => Set<RcuReport>();

    public DbSet<Classification> Classifications => Set<Classification>();

    public DbSet<ExistingLoan> ExistingLoans => Set<ExistingLoan>();

    public DbSet<BankingRecord> BankingRecords => Set<BankingRecord>();

    public DbSet<EligibilityDecision> EligibilityDecisions => Set<EligibilityDecision>();

    public DbSet<Business> Businesses => Set<Business>();

    public DbSet<Partner> Partners => Set<Partner>();

    public DbSet<Tvr> Tvrs => Set<Tvr>();

    public DbSet<ApprovalDecision> ApprovalDecisions => Set<ApprovalDecision>();

    public DbSet<Charge> Charges => Set<Charge>();

    public DbSet<SendBackLog> SendBackLogs => Set<SendBackLog>();

    public DbSet<Disbursement> Disbursements => Set<Disbursement>();

    public DbSet<EnachMandate> EnachMandates => Set<EnachMandate>();

    public DbSet<SecurityNachMandate> SecurityNachMandates => Set<SecurityNachMandate>();

    public DbSet<DownPaymentRecord> DownPaymentRecords => Set<DownPaymentRecord>();

    public DbSet<PostSanctionChecklist> PostSanctionChecklists => Set<PostSanctionChecklist>();

    public DbSet<Pdd> Pdds => Set<Pdd>();

    public DbSet<RejectionLog> RejectionLogs => Set<RejectionLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Every child table has a required navigation to Application, which IS query-filtered, and EF
        // warns that a required principal can be filtered away. Here that is the entire point: a child
        // row belonging to another company must vanish along with the application that owns it.
        // Configured here rather than at each registration so the design-time factory gets it too.
        optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(
            CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureTenancy(modelBuilder);

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(a => a.Id);

            // MySQL needs explicit precision or EF warns and silently truncates.
            entity.Property(a => a.LoanAmount).HasPrecision(18, 2);
            entity.Property(a => a.ProcessingFee).HasPrecision(18, 2);
            entity.Property(a => a.AdvanceEmi).HasPrecision(18, 2);

            // Percentage, e.g. 13.25 — two decimal places is all a rate needs.
            entity.Property(a => a.Roi).HasPrecision(5, 2);

            entity.Property(a => a.CurrentStage).HasDefaultValue(1);
            entity.Property(a => a.Status).HasDefaultValue("New");

            // Restrict, not cascade: deleting an officer must never erase which files they held.
            entity.HasOne(a => a.AssignedOfficerNavigation)
                .WithMany()
                .HasForeignKey(a => a.AssignedOfficerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Restrict, not cascade: deleting a company must be a deliberate act, never something
            // that silently takes every loan file it owns with it.
            entity.HasOne(a => a.Company)
                .WithMany()
                .HasForeignKey(a => a.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => a.CompanyId);

            // No HasData here. Applications and all their child rows come from DemoSeedData at
            // startup instead: they need Identity user ids that do not exist at migration time, and
            // queue dates relative to today so the dashboard's ageing stays honest.
        });

        modelBuilder.Entity<Party>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.MonthlyIncome).HasPrecision(18, 2);
            entity.Property(p => p.DedupeStatus).HasDefaultValue("NotRun");

            // One KYC record per party type per application.
            entity.HasIndex(p => new { p.ApplicationId, p.PartyType }).IsUnique();

            entity.HasOne(p => p.Application)
                .WithMany(a => a.Parties)
                .HasForeignKey(p => p.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SecurityDetail>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.InvoiceValue).HasPrecision(18, 2);
            entity.Property(s => s.AssessedValue).HasPrecision(18, 2);
            entity.Property(s => s.Area).HasPrecision(18, 2);

            // One security record per application.
            entity.HasIndex(s => s.ApplicationId).IsUnique();

            entity.HasOne(s => s.Application)
                .WithOne()
                .HasForeignKey<SecurityDetail>(s => s.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reference>(entity =>
        {
            entity.HasKey(r => r.Id);

            // Many per application — no unique index here, unlike the other two.
            entity.HasIndex(r => r.ApplicationId);

            entity.HasOne(r => r.Application)
                .WithMany()
                .HasForeignKey(r => r.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Viability>(entity =>
        {
            // The DbSet is plural for convention's sake; the table is not.
            entity.ToTable("Viability");

            entity.HasKey(v => v.Id);

            entity.Property(v => v.IncomeFreight).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(v => v.IncomeSalary).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(v => v.IncomeOther).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(v => v.ExpenseHousehold).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(v => v.ExpenseFuelDriver).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(v => v.ExpenseExistingEmi).HasPrecision(18, 2).HasDefaultValue(0m);

            entity.HasIndex(v => v.ApplicationId).IsUnique();

            entity.HasOne(v => v.Application)
                .WithOne()
                .HasForeignKey<Viability>(v => v.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BankDetail>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.PennyDropStatus).HasDefaultValue("NotRun");

            entity.HasIndex(b => b.ApplicationId).IsUnique();

            entity.HasOne(b => b.Application)
                .WithOne()
                .HasForeignKey<BankDetail>(b => b.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BankStatement>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.ParsedStatus).HasDefaultValue("NotConfigured");

            // Many per application — not unique, unlike the other three.
            entity.HasIndex(s => s.ApplicationId);

            entity.HasOne(s => s.Application)
                .WithMany()
                .HasForeignKey(s => s.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CamCostBreakdown>(entity =>
        {
            entity.ToTable("CamCostBreakdown");
            entity.HasKey(c => c.Id);

            foreach (var column in new[]
            {
                nameof(CamCostBreakdown.DraftExShowroomCost),
                nameof(CamCostBreakdown.DraftBodyAccessories),
                nameof(CamCostBreakdown.DraftInsuranceRegistration),
                nameof(CamCostBreakdown.DraftMargin),
                nameof(CamCostBreakdown.AppliedExShowroomCost),
                nameof(CamCostBreakdown.AppliedBodyAccessories),
                nameof(CamCostBreakdown.AppliedInsuranceRegistration),
                nameof(CamCostBreakdown.AppliedMargin),
            })
            {
                entity.Property<decimal?>(column).HasPrecision(18, 2);
            }

            entity.HasIndex(c => c.ApplicationId).IsUnique();

            entity.HasOne(c => c.Application)
                .WithOne()
                .HasForeignKey<CamCostBreakdown>(c => c.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChecklistDocument>(entity =>
        {
            // Entity is ChecklistDocument to avoid colliding with QuestPDF's Document; the table
            // keeps the name the brief specifies.
            entity.ToTable("Documents");
            entity.HasKey(d => d.Id);

            // One row per application, party and document type.
            entity.HasIndex(d => new { d.ApplicationId, d.PartyType, d.DocumentType }).IsUnique();

            entity.HasOne(d => d.Application)
                .WithMany()
                .HasForeignKey(d => d.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentRemark>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.DocumentId);

            entity.HasOne(r => r.Document)
                .WithMany()
                .HasForeignKey(r => r.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RcuInitiation>(entity =>
        {
            // Singular table name, as specified.
            entity.ToTable("RcuInitiation");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Mode).HasDefaultValue("Screened");

            entity.HasIndex(i => i.ApplicationId).IsUnique();

            entity.HasOne(i => i.Application)
                .WithOne()
                .HasForeignKey<RcuInitiation>(i => i.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict, not cascade: deleting an officer must never silently erase the audit trail
            // of who approved an override.
            entity.HasOne(i => i.OverrideApprover)
                .WithMany()
                .HasForeignKey(i => i.OverrideApproverOfficerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RcuOutcome>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Status).HasDefaultValue("Pending");

            entity.HasIndex(o => new { o.ApplicationId, o.PartyType }).IsUnique();

            entity.HasOne(o => o.Application)
                .WithMany()
                .HasForeignKey(o => o.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(o => o.VerifiedBy)
                .WithMany()
                .HasForeignKey(o => o.VerifiedByOfficerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RcuReport>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => new { r.ApplicationId, r.SequenceNumber });

            entity.HasOne(r => r.Application)
                .WithMany()
                .HasForeignKey(r => r.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Classification>(entity =>
        {
            // Singular table name, as specified.
            entity.ToTable("Classification");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Psl).HasDefaultValue(Classification.PslNo);

            entity.Property(c => c.RiskSharing).HasPrecision(5, 2);
            entity.Property(c => c.PrioritySectorAmount).HasPrecision(18, 2);

            entity.HasIndex(c => c.ApplicationId).IsUnique();

            entity.HasOne(c => c.Application)
                .WithOne()
                .HasForeignKey<Classification>(c => c.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExistingLoan>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Rtr).HasDefaultValue("Regular");

            entity.Property(l => l.Sanctioned).HasPrecision(18, 2);
            entity.Property(l => l.Pos).HasPrecision(18, 2);
            entity.Property(l => l.Emi).HasPrecision(18, 2);
            entity.Property(l => l.Roi).HasPrecision(5, 2);

            entity.HasIndex(l => l.ApplicationId);

            entity.HasOne(l => l.Application)
                .WithMany()
                .HasForeignKey(l => l.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BankingRecord>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.AvgBalance).HasPrecision(18, 2);
            entity.Property(b => b.InwardPct).HasPrecision(5, 2);
            entity.Property(b => b.OutwardPct).HasPrecision(5, 2);

            entity.HasIndex(b => b.ApplicationId);

            entity.HasOne(b => b.Application)
                .WithMany()
                .HasForeignKey(b => b.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EligibilityDecision>(entity =>
        {
            // Singular table name, matching Classification and RcuInitiation.
            entity.ToTable("EligibilityDecision");
            entity.HasKey(d => d.Id);

            // Signed: the stored deviation is negative whenever a note was actually required.
            entity.Property(d => d.NoteWrittenAtDeviationPct).HasPrecision(7, 2);

            entity.HasIndex(d => d.ApplicationId).IsUnique();

            entity.HasOne(d => d.Application)
                .WithOne()
                .HasForeignKey<EligibilityDecision>(d => d.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Business>(entity =>
        {
            // Singular table name, as specified.
            entity.ToTable("Business");
            entity.HasKey(b => b.Id);

            entity.Property(b => b.Turnover).HasPrecision(18, 2);

            entity.HasIndex(b => b.ApplicationId).IsUnique();

            entity.HasOne(b => b.Application)
                .WithOne()
                .HasForeignKey<Business>(b => b.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Partner>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Shareholding).HasPrecision(5, 2);

            entity.HasIndex(p => p.ApplicationId);

            entity.HasOne(p => p.Application)
                .WithMany()
                .HasForeignKey(p => p.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tvr>(entity =>
        {
            entity.ToTable("Tvr");
            entity.HasKey(t => t.Id);

            entity.HasIndex(t => t.ApplicationId).IsUnique();

            entity.HasOne(t => t.Application)
                .WithOne()
                .HasForeignKey<Tvr>(t => t.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApprovalDecision>(entity =>
        {
            entity.ToTable("ApprovalDecision");
            entity.HasKey(d => d.Id);

            entity.Property(d => d.SanctionedAmount).HasPrecision(18, 2);
            entity.Property(d => d.SanctionedRoi).HasPrecision(5, 2);
            entity.Property(d => d.SanctionConfirmed).HasDefaultValue(false);

            entity.HasIndex(d => d.ApplicationId).IsUnique();

            entity.HasOne(d => d.Application)
                .WithOne()
                .HasForeignKey<ApprovalDecision>(d => d.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict, not cascade: removing a user must never quietly unsign a sanction.
            entity.HasOne(d => d.Recommender)
                .WithMany()
                .HasForeignKey(d => d.RecommenderUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Approver)
                .WithMany()
                .HasForeignKey(d => d.ApproverUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Charge>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Amount).HasPrecision(18, 2);
            entity.Property(c => c.Gst).HasPrecision(18, 2);
            entity.Property(c => c.Locked).HasDefaultValue(false);
            entity.Property(c => c.Waived).HasDefaultValue(false);

            entity.HasIndex(c => c.ApplicationId);

            entity.HasOne(c => c.Application)
                .WithMany()
                .HasForeignKey(c => c.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SendBackLog>(entity =>
        {
            // Singular table name, as specified.
            entity.ToTable("SendBackLog");
            entity.HasKey(s => s.Id);

            entity.HasIndex(s => s.ApplicationId);

            entity.HasOne(s => s.Application)
                .WithMany()
                .HasForeignKey(s => s.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RejectionLog>(entity =>
        {
            // Singular table name, matching SendBackLog and RcuInitiation.
            entity.ToTable("RejectionLog");
            entity.HasKey(r => r.Id);

            entity.HasIndex(r => r.ApplicationId);

            entity.HasOne(r => r.Application)
                .WithMany()
                .HasForeignKey(r => r.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Disbursement>(entity =>
        {
            // Singular table name, as specified.
            entity.ToTable("Disbursement");
            entity.HasKey(d => d.Id);

            entity.HasIndex(d => d.ApplicationId).IsUnique();

            entity.HasOne(d => d.Application)
                .WithOne()
                .HasForeignKey<Disbursement>(d => d.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EnachMandate>(entity =>
        {
            entity.ToTable("EnachMandate");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Status).HasDefaultValue("Pending");
            entity.Property(m => m.NameMatchStatus).HasDefaultValue("NotRun");
            entity.Property(m => m.ConfirmationAccepted).HasDefaultValue(false);

            entity.HasIndex(m => m.ApplicationId).IsUnique();

            entity.HasOne(m => m.Application)
                .WithOne()
                .HasForeignKey<EnachMandate>(m => m.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SecurityNachMandate>(entity =>
        {
            entity.ToTable("SecurityNachMandate");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Status).HasDefaultValue("Pending");
            entity.Property(m => m.NameMatchStatus).HasDefaultValue("NotRun");
            entity.Property(m => m.ConfirmationAccepted).HasDefaultValue(false);

            entity.HasIndex(m => m.ApplicationId).IsUnique();

            entity.HasOne(m => m.Application)
                .WithOne()
                .HasForeignKey<SecurityNachMandate>(m => m.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DownPaymentRecord>(entity =>
        {
            entity.ToTable("DownPaymentRecord");
            entity.HasKey(d => d.Id);

            entity.Property(d => d.AmountReceived).HasPrecision(18, 2);

            entity.HasIndex(d => d.ApplicationId).IsUnique();

            entity.HasOne(d => d.Application)
                .WithOne()
                .HasForeignKey<DownPaymentRecord>(d => d.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostSanctionChecklist>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Flag).HasDefaultValue("Pending");

            entity.HasIndex(c => c.ApplicationId);

            entity.HasOne(c => c.Application)
                .WithMany()
                .HasForeignKey(c => c.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pdd>(entity =>
        {
            entity.ToTable("Pdd");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Status).HasDefaultValue("Open");

            entity.HasIndex(p => p.ApplicationId);

            entity.HasOne(p => p.Application)
                .WithMany()
                .HasForeignKey(p => p.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict, not cascade: deleting an officer must never erase who approved a waiver.
            entity.HasOne(p => p.WaivedBy)
                .WithMany()
                .HasForeignKey(p => p.WaivedByOfficerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IfscBankLookup>(entity =>
        {
            entity.ToTable("IfscBankLookup");
            entity.HasKey(i => i.IfscPrefix);

            // PARTIAL LIST — twelve common prefixes, not the RBI-published dataset. See the remarks
            // on IfscBankLookup; the real list is v2 scope.
            entity.HasData(
                new IfscBankLookup { IfscPrefix = "HDFC", BankName = "HDFC Bank" },
                new IfscBankLookup { IfscPrefix = "ICIC", BankName = "ICICI Bank" },
                new IfscBankLookup { IfscPrefix = "SBIN", BankName = "State Bank of India" },
                new IfscBankLookup { IfscPrefix = "UTIB", BankName = "Axis Bank" },
                new IfscBankLookup { IfscPrefix = "BARB", BankName = "Bank of Baroda" },
                new IfscBankLookup { IfscPrefix = "PUNB", BankName = "Punjab National Bank" },
                new IfscBankLookup { IfscPrefix = "KKBK", BankName = "Kotak Mahindra Bank" },
                new IfscBankLookup { IfscPrefix = "YESB", BankName = "Yes Bank" },
                new IfscBankLookup { IfscPrefix = "IDFB", BankName = "IDFC First Bank" },
                new IfscBankLookup { IfscPrefix = "INDB", BankName = "IndusInd Bank" },
                new IfscBankLookup { IfscPrefix = "CNRB", BankName = "Canara Bank" },
                new IfscBankLookup { IfscPrefix = "UBIN", BankName = "Union Bank of India" });
        });
    }

    /// <summary>
    /// Companies, branches, and the three global query filters that make tenant isolation structural.
    /// </summary>
    /// <remarks>
    /// Only three entities carry a filter. Everything else — Parties, Documents, Viability, RcuOutcomes
    /// and the rest — is reachable only through an <see cref="Application"/> that has already been
    /// filtered, so it inherits the isolation instead of repeating it. That inheritance is real but it
    /// is a property of how the screens query: each stage page loads its application first and bails
    /// out when the lookup returns null. Any future code that reaches a child table WITHOUT first
    /// resolving its application would bypass this and must scope itself explicitly.
    ///
    /// <see cref="IfscBankLookup"/> is deliberately unfiltered: it is global reference data, not
    /// anybody's property.
    /// </remarks>
    private void ConfigureTenancy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(c => c.Id);

            // No query filter. A company row is only ever fetched by the id in the caller's own claim,
            // and a SuperAdmin has to be able to enumerate every one of them.

            // The tenant every pre-existing row belongs to. Seeded here rather than at runtime because
            // it has to exist before the 128 seeded applications that reference it. CreatedAt is a
            // fixed date, not DateTime.UtcNow — a moving value would make every migration regenerate.
            entity.Property(c => c.FoirCapPct).HasPrecision(5, 2);
            entity.Property(c => c.LtvCapPct).HasPrecision(5, 2);
            entity.Property(c => c.FoirRiskCautionPct).HasPrecision(5, 2);
            entity.Property(c => c.FoirRiskDangerPct).HasPrecision(5, 2);
            entity.Property(c => c.LtvRiskCautionPct).HasPrecision(5, 2);
            entity.Property(c => c.LtvRiskDangerPct).HasPrecision(5, 2);
            entity.Property(c => c.GstPct).HasPrecision(5, 2);
            entity.Property(c => c.NoteStaleTolerancePct).HasPrecision(5, 2);

            // Every policy value below is the exact literal it replaced in code, so relocating them
            // changed no behaviour. They are the defaults on the model too, which is what a company
            // created later starts from.
            entity.HasData(new Company
            {
                Id = SeedCompanyId,
                Name = "Default Company — rename in Company Setup",
                SlaOverdueDays = 5,
                FoirCapPct = 50m,
                LtvCapPct = 85m,
                FoirRiskCautionPct = 40m,
                FoirRiskDangerPct = 60m,
                LtvRiskCautionPct = 75m,
                LtvRiskDangerPct = 90m,
                GstPct = 18m,
                CibilMinScore = 300,
                CibilMaxScore = 900,
                AddressValidityDays = 90,
                NoteStaleTolerancePct = 0.5m,
                MinimumReferences = 2,
                CreatedAt = new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc),
            });
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.HasIndex(b => b.CompanyId);

            entity.HasOne(b => b.Company)
                .WithMany()
                .HasForeignKey(b => b.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasData(
                new Branch { Id = 1, CompanyId = SeedCompanyId, Name = "Nashik West", IsActive = true },
                new Branch { Id = 2, CompanyId = SeedCompanyId, Name = "Nashik East", IsActive = true },
                new Branch { Id = 3, CompanyId = SeedCompanyId, Name = "Pune Camp", IsActive = true },
                new Branch { Id = 4, CompanyId = SeedCompanyId, Name = "Aurangabad", IsActive = true },
                new Branch { Id = 5, CompanyId = SeedCompanyId, Name = "Jalgaon", IsActive = true });
        });

        modelBuilder.Entity<VehicleLoanCap>(entity =>
        {
            entity.HasKey(v => v.Id);

            entity.Property(v => v.MaxLoanAmount).HasPrecision(18, 2);

            // One cap per make/model/YEAR per company — the catalog is also the dropdown source, so a
            // duplicate would render twice and leave the cap ambiguous. Year is in the key because a
            // model's ceiling drops as it ages; without it one number would have to cover every
            // model year at once.
            entity.HasIndex(v => new { v.CompanyId, v.Make, v.Model, v.Year }).IsUnique();

            entity.HasOne(v => v.Company)
                .WithMany()
                .HasForeignKey(v => v.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(u => u.CompanyId);

            entity.HasOne(u => u.Company)
                .WithMany()
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---- The filters ----
        //
        // Anonymous requests get a null _companyId. Application.CompanyId is non-nullable, so the
        // comparison is never true and an unauthenticated query returns nothing at all. Failing
        // closed is the point: a bug in the auth wiring shows up as an empty dashboard, not as a
        // cross-tenant leak.
        modelBuilder.Entity<Application>()
            .HasQueryFilter(a => _isSuperAdmin || a.CompanyId == _companyId);

        modelBuilder.Entity<Branch>()
            .HasQueryFilter(b => _isSuperAdmin || b.CompanyId == _companyId);

        modelBuilder.Entity<VehicleLoanCap>()
            .HasQueryFilter(v => _isSuperAdmin || v.CompanyId == _companyId);

        // The one deliberate exception, and it is narrow: when nobody is signed in the user filter is
        // open, because Identity's own sign-in path calls FindByEmailAsync on this very context before
        // a company can possibly be known. Email is a global identifier, so that lookup has to see
        // every company. Nothing else is reachable while anonymous — the authorization fallback policy
        // requires an authenticated user for every endpoint except the /account pages.
        modelBuilder.Entity<ApplicationUser>()
            .HasQueryFilter(u => !_hasUser || _isSuperAdmin || u.CompanyId == _companyId);
    }

    /// <summary>Id of the single company created by <c>HasData</c> and owned by every seeded row.</summary>
    public const int SeedCompanyId = 1;

    public override int SaveChanges()
    {
        StampTimestamps();
        StampTenant();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        StampTenant();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Stamps the owning company onto every newly inserted tenant-scoped row, from the signed-in
    /// user's claims.
    /// </summary>
    /// <remarks>
    /// Here rather than at each call site so it cannot be forgotten, and it overwrites whatever the
    /// caller set — CompanyId is never accepted as input, from a form, a URL or anywhere else.
    ///
    /// A SuperAdmin has no company of their own, so rows they create keep whatever CompanyId was
    /// assigned explicitly; that is the only path allowed to choose, and it exists so cross-company
    /// administration is possible at all.
    /// </remarks>
    private void StampTenant()
    {
        if (_companyId is not { } companyId)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added)
            {
                continue;
            }

            switch (entry.Entity)
            {
                case Application application:
                    application.CompanyId = companyId;
                    break;
                case Branch branch:
                    branch.CompanyId = companyId;
                    break;
                case VehicleLoanCap cap:
                    cap.CompanyId = companyId;
                    break;
                case ApplicationUser user:
                    user.CompanyId = companyId;
                    break;
            }
        }
    }

    /// <summary>
    /// Maintains CreatedAt / UpdatedAt in application code rather than with a database trigger,
    /// so the behaviour is visible and testable alongside the rest of the model.
    /// </summary>
    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            switch (entry.Entity)
            {
                case Company company:
                    if (entry.State == EntityState.Added && company.CreatedAt == default)
                    {
                        company.CreatedAt = now;
                    }

                    company.UpdatedAt = now;
                    break;

                case VehicleLoanCap vehicleCap:
                    if (entry.State == EntityState.Added && vehicleCap.CreatedAt == default)
                    {
                        vehicleCap.CreatedAt = now;
                    }

                    vehicleCap.UpdatedAt = now;
                    break;

                case Application application:
                    if (entry.State == EntityState.Added && application.CreatedAt == default)
                    {
                        application.CreatedAt = now;
                    }

                    application.UpdatedAt = now;
                    break;

                case Party party:
                    if (entry.State == EntityState.Added && party.CreatedAt == default)
                    {
                        party.CreatedAt = now;
                    }

                    party.UpdatedAt = now;
                    break;

                case SecurityDetail security:
                    if (entry.State == EntityState.Added && security.CreatedAt == default)
                    {
                        security.CreatedAt = now;
                    }

                    security.UpdatedAt = now;
                    break;

                case Viability viability:
                    if (entry.State == EntityState.Added && viability.CreatedAt == default)
                    {
                        viability.CreatedAt = now;
                    }

                    viability.UpdatedAt = now;
                    break;

                case BankDetail bank:
                    if (entry.State == EntityState.Added && bank.CreatedAt == default)
                    {
                        bank.CreatedAt = now;
                    }

                    bank.UpdatedAt = now;
                    break;

                case CamCostBreakdown cam:
                    if (entry.State == EntityState.Added && cam.CreatedAt == default)
                    {
                        cam.CreatedAt = now;
                    }

                    cam.UpdatedAt = now;
                    break;

                // References carry CreatedAt only — they are replaced wholesale, never updated.
                case Reference reference:
                    if (entry.State == EntityState.Added && reference.CreatedAt == default)
                    {
                        reference.CreatedAt = now;
                    }

                    break;

                case RcuInitiation initiation:
                    if (entry.State == EntityState.Added && initiation.CreatedAt == default)
                    {
                        initiation.CreatedAt = now;
                    }

                    initiation.UpdatedAt = now;
                    break;

                case RcuOutcome outcome:
                    if (entry.State == EntityState.Added && outcome.CreatedAt == default)
                    {
                        outcome.CreatedAt = now;
                    }

                    outcome.UpdatedAt = now;
                    break;

                // Reports are insert-only rounds; an existing round is never rewritten except to
                // attach its file, which does not change when the round was raised.
                case RcuReport report:
                    if (entry.State == EntityState.Added && report.CreatedAt == default)
                    {
                        report.CreatedAt = now;
                    }

                    break;

                case ChecklistDocument document:
                    if (entry.State == EntityState.Added && document.CreatedAt == default)
                    {
                        document.CreatedAt = now;
                    }

                    document.UpdatedAt = now;
                    break;

                // Remarks are append-only — CreatedAt is the log entry's date and never changes.
                case DocumentRemark remark:
                    if (entry.State == EntityState.Added && remark.CreatedAt == default)
                    {
                        remark.CreatedAt = now;
                    }

                    break;

                case Classification classification:
                    if (entry.State == EntityState.Added && classification.CreatedAt == default)
                    {
                        classification.CreatedAt = now;
                    }

                    classification.UpdatedAt = now;
                    break;

                case ExistingLoan existingLoan:
                    if (entry.State == EntityState.Added && existingLoan.CreatedAt == default)
                    {
                        existingLoan.CreatedAt = now;
                    }

                    existingLoan.UpdatedAt = now;
                    break;

                case BankingRecord bankingRecord:
                    if (entry.State == EntityState.Added && bankingRecord.CreatedAt == default)
                    {
                        bankingRecord.CreatedAt = now;
                    }

                    bankingRecord.UpdatedAt = now;
                    break;

                case EligibilityDecision decision:
                    if (entry.State == EntityState.Added && decision.CreatedAt == default)
                    {
                        decision.CreatedAt = now;
                    }

                    decision.UpdatedAt = now;
                    break;

                case Business business:
                    if (entry.State == EntityState.Added && business.CreatedAt == default)
                    {
                        business.CreatedAt = now;
                    }

                    business.UpdatedAt = now;
                    break;

                case Partner partner:
                    if (entry.State == EntityState.Added && partner.CreatedAt == default)
                    {
                        partner.CreatedAt = now;
                    }

                    partner.UpdatedAt = now;
                    break;

                case Tvr tvr:
                    if (entry.State == EntityState.Added && tvr.CreatedAt == default)
                    {
                        tvr.CreatedAt = now;
                    }

                    tvr.UpdatedAt = now;
                    break;

                case ApprovalDecision approval:
                    if (entry.State == EntityState.Added && approval.CreatedAt == default)
                    {
                        approval.CreatedAt = now;
                    }

                    approval.UpdatedAt = now;
                    break;

                case Charge charge:
                    if (entry.State == EntityState.Added && charge.CreatedAt == default)
                    {
                        charge.CreatedAt = now;
                    }

                    charge.UpdatedAt = now;
                    break;

                // Send-back entries are append-only — CreatedAt is when it happened, never rewritten.
                case SendBackLog sendBack:
                    if (entry.State == EntityState.Added && sendBack.CreatedAt == default)
                    {
                        sendBack.CreatedAt = now;
                    }

                    break;

                // Rejections are a point in time, stamped on insert and never rewritten.
                case RejectionLog rejection:
                    if (entry.State == EntityState.Added && rejection.RejectedAt == default)
                    {
                        rejection.RejectedAt = now;
                    }

                    break;

                case Disbursement disbursement:
                    if (entry.State == EntityState.Added && disbursement.CreatedAt == default)
                    {
                        disbursement.CreatedAt = now;
                    }

                    disbursement.UpdatedAt = now;
                    break;

                case EnachMandate enach:
                    if (entry.State == EntityState.Added && enach.CreatedAt == default)
                    {
                        enach.CreatedAt = now;
                    }

                    enach.UpdatedAt = now;
                    break;

                case SecurityNachMandate securityNach:
                    if (entry.State == EntityState.Added && securityNach.CreatedAt == default)
                    {
                        securityNach.CreatedAt = now;
                    }

                    securityNach.UpdatedAt = now;
                    break;

                case DownPaymentRecord downPayment:
                    if (entry.State == EntityState.Added && downPayment.CreatedAt == default)
                    {
                        downPayment.CreatedAt = now;
                    }

                    downPayment.UpdatedAt = now;
                    break;

                case PostSanctionChecklist checklistItem:
                    if (entry.State == EntityState.Added && checklistItem.CreatedAt == default)
                    {
                        checklistItem.CreatedAt = now;
                    }

                    checklistItem.UpdatedAt = now;
                    break;

                case Pdd pdd:
                    if (entry.State == EntityState.Added && pdd.CreatedAt == default)
                    {
                        pdd.CreatedAt = now;
                    }

                    pdd.UpdatedAt = now;
                    break;

                // Statements are insert-only; an existing row is never rewritten.
                case BankStatement statement:
                    if (entry.State == EntityState.Added)
                    {
                        if (statement.CreatedAt == default)
                        {
                            statement.CreatedAt = now;
                        }

                        if (statement.UploadedOn == default)
                        {
                            statement.UploadedOn = now;
                        }
                    }

                    break;
            }
        }
    }
}

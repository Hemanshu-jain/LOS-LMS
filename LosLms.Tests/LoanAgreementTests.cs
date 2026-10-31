using LosLms.Models;
using LosLms.Services;
using QuestPDF.Fluent;
using Xunit;

namespace LosLms.Tests;

public sealed class LoanAgreementTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    private static List<AmortizationRow> Schedule(decimal principal, decimal rate, int months, DateOnly first) =>
        LoanMath.Schedule(principal, rate, months, first, null, null);

    // ---- Key Fact Statement maths ----

    [Fact]
    public void Kfs_TotalInterestIsTheSumOfTheScheduleInterest()
    {
        var schedule = Schedule(100_000m, 12m, 12, D(2026, 12, 5));

        var kfs = LoanKfs.Compute(schedule, 100_000m, 100_000m, 0m, D(2026, 11, 5));

        Assert.Equal(12, kfs.Instalments);
        Assert.InRange(kfs.TotalInterest, 6600m, 6640m);                 // 12% p.a., 12 months: about 6,618.55
        Assert.Equal(Math.Round(schedule.Sum(r => r.Emi), 2), kfs.TotalRepayable);
        Assert.InRange(kfs.TotalRepayable - kfs.TotalInterest, 99_990m, 100_010m);
    }

    [Fact]
    public void Kfs_AprWithNoFeesIsTheEffectiveRateOfTheNominalRate()
    {
        var schedule = Schedule(100_000m, 12m, 12, D(2026, 12, 5));

        var apr = LoanKfs.Apr(schedule, 100_000m, D(2026, 11, 5));

        // 1% a month compounds to 12.68% a year.
        Assert.NotNull(apr);
        Assert.InRange(apr!.Value, 12.6m, 12.8m);
    }

    [Fact]
    public void Kfs_FeesDeductedUpFrontRaiseTheApr()
    {
        var schedule = Schedule(100_000m, 12m, 12, D(2026, 12, 5));

        var without = LoanKfs.Apr(schedule, 100_000m, D(2026, 11, 5));
        var with = LoanKfs.Apr(schedule, 97_000m, D(2026, 11, 5));

        Assert.True(with > without + 2m, $"{with} should be well above {without}");
    }

    [Fact]
    public void Kfs_AprIsNullWhenThereIsNothingToMeasure()
    {
        Assert.Null(LoanKfs.Apr(new List<AmortizationRow>(), 100_000m, null));
        Assert.Null(LoanKfs.Apr(Schedule(100_000m, 12m, 12, D(2026, 12, 5)), 0m, null));
    }

    // ---- naming ----

    [Fact]
    public void DocumentNames_FollowTheOwnersPattern()
    {
        var name = DocumentNames.For("KIS-2026-000010", "Hemanshu Jain", new DateTime(2026, 10, 16, 14, 3, 0), DocumentNames.LoanAgreement);
        var signed = DocumentNames.For("KIS-2026-000010", "Hemanshu Jain", new DateTime(2026, 10, 16), DocumentNames.LoanAgreementSigned);

        Assert.Equal("Hemanshu-Jain_2026-10-16_KIS-2026-000010_Loan-Agreement.pdf", name);
        Assert.Equal("Hemanshu-Jain_2026-10-16_KIS-2026-000010_Loan-Agreement-SIGNED.pdf", signed);
    }

    [Theory]
    [InlineData("  Raj  K. D'Souza / Sons ", "Raj-K-D-Souza-Sons")]
    [InlineData("../..\\evil", "evil")]
    [InlineData(null, "Customer")]
    [InlineData("!!!", "Customer")]
    public void DocumentNames_AreFileSafe(string? input, string expected) =>
        Assert.Equal(expected, DocumentNames.Safe(input, "Customer"));

    // ---- clauses ----

    [Fact]
    public void Clauses_HaveAGuaranteeAndCrossReferencesPointAtTheRightClauses()
    {
        var clauses = AgreementClauses.For(new ClauseContext("Kishore Finance", "Nashik", "Security text.", "S. Kulkarni."));

        Assert.Equal(Enumerable.Range(1, clauses.Count), clauses.Select(c => c.Number));
        var guarantee = Assert.Single(clauses, c => c.Heading == "Guarantee");
        Assert.Contains("continuing guarantee", string.Join(' ', guarantee.Paragraphs));
        Assert.Contains("co-extensive", string.Join(' ', guarantee.Paragraphs));
        Assert.Contains("recovery", clauses.Single(c => c.Number == AgreementClauses.RecoveryClause).Heading, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Electronic execution", clauses.Single(c => c.Number == AgreementClauses.ElectronicExecutionClause).Heading);
    }

    // ---- masking ----

    [Fact]
    public void Masking_ShowsOnlyWhatTheOwnerAllows()
    {
        Assert.Equal("XXXXX1234X", LoanAgreementPdf.MaskPan("abcde1234f"));
        Assert.Equal("—", LoanAgreementPdf.MaskPan(null));
        Assert.Equal("XXXX XXXX 5094", LoanAgreementPdf.MaskAadhaar("2345 6789 5094"));
        Assert.Equal("—", LoanAgreementPdf.MaskAadhaar("12"));
    }

    // ---- signature boxes ----

    [Fact]
    public void SignatureBoxes_AreFixedOnTheSignaturePage_AndDoNotOverlap()
    {
        var boxes = new LoanAgreementSignatureBoxes();
        var a = Assert.Single(boxes.BoxesFor("Applicant")!);
        var c = Assert.Single(boxes.BoxesFor("CoApplicant")!);
        var g = Assert.Single(boxes.BoxesFor("Guarantor")!);

        Assert.All(new[] { a, c, g }, b => Assert.Equal(AgreementLayout.SignaturePage, b.Page));
        Assert.True(a.Lly > c.Ury && c.Lly > g.Ury, "boxes run top to bottom without touching");
        Assert.All(new[] { a, c, g }, b =>
        {
            Assert.InRange(b.Llx, 0, 595);
            Assert.InRange(b.Urx, b.Llx + 100, 595);
            Assert.InRange(b.Lly, 40, 842);
            Assert.InRange(b.Ury, b.Lly + 40, 842);
        });
        Assert.Null(boxes.BoxesFor("Nobody"));
    }

    // ---- the document ----

    internal static AgreementData Data(bool worstCase, int partyCount = 3)
    {
        var company = new Company
        {
            Name = worstCase ? "Kishore Finance and Leasing Private Limited (formerly Kishore Hire Purchase Company)" : "Kishore Finance Pvt Ltd",
            Address = worstCase
                ? "Plot 14, Survey No. 112/3, Opposite Nashik Road Railway Station, Near Jail Road Chowk, Nashik Road, Nashik, Maharashtra 422101"
                : "12 Main Road, Nashik, Maharashtra 422001",
            RegisteredCity = "Nashik", Cin = "U65921MH2015PTC123456", RbiNbfcRegNo = "N-13.01234", Gstin = "27AAAAA0000A1Z5",
            ContactPhone = "0253-2345678", ContactEmail = "care@kishorefinance.example", Website = "www.kishorefinance.example",
            GrievanceOfficerName = "S. Kulkarni", GrievanceOfficerPhone = "0253-2345600", GrievanceOfficerEmail = "grievance@kishorefinance.example",
            AuthorisedSignatoryName = "R. Kishore", AuthorisedSignatoryDesignation = "Director",
        };
        var app = new Application { Id = "KIS-2026-000010", CustomerName = "Hemanshu Jain", LoanProduct = "Commercial vehicle loan", CreatedAt = new DateTime(2026, 10, 16) };
        var decision = new ApprovalDecision { SanctionedAmount = 1_400_000m, SanctionedRoi = 13.5m, SanctionedTenure = 48 };
        var longAddress = worstCase ? "Flat 402, Building C, Shree Samarth Heights Co-operative Housing Society, Behind Municipal Corporation Office" : "12 Gandhi Road";
        Party P(string type, string name) => new()
        {
            Id = type.Length, PartyType = type, FullName = name, FatherSpouseName = "Late Mohanlal Jain", DateOfBirth = D(1985, 3, 9),
            Address1 = longAddress, Address2 = "Near Water Tank", City = "Nashik", State = "Maharashtra", PinCode = "422011",
            Pan = "ABCPJ1234F", Aadhaar = "234567895094", Mobile = "9876543210", Email = "someone@example.com",
        };
        var parties = new[] { P("Applicant", "Hemanshu Jain"), P("CoApplicant", "Sneha Jain"), P("Guarantor", "Digambar Jadhav") }.Take(partyCount).ToList();
        var security = worstCase
            ? new SecurityDetail { AssetType = "Property", PropertyType = "Residential flat", PropertyAddress = string.Concat(Enumerable.Repeat("Flat 402, Shree Samarth Heights, Gangapur Road, Nashik 422013. ", 6)) }
            : new SecurityDetail { AssetType = "Vehicle", MakeModel = "Tata Ace Gold", MfgYear = "2026", RegNo = "MH15AB1234", ChassisNo = "MAT123456789", EngineNo = "ENG998877" };
        var charges = new List<Charge>
        {
            new() { Head = "Processing fee", Amount = 21_000m, Gst = 3_780m },
            new() { Head = "Documentation charges", Amount = 1_500m, Gst = 270m },
            new() { Head = "Stamp duty", Amount = 700m, Gst = 0m, Waived = true },
        };
        var schedule = Schedule(1_400_000m, 13.5m, 48, D(2026, 12, 5));
        return new AgreementData(company, app, decision, parties, security, charges, schedule, schedule[0].Emi, 1_400_000m - 26_550m, 0m,
            D(2026, 11, 5), D(2026, 12, 5), 5, 500m, 18m, D(2026, 10, 31), new byte[] { });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Agreement_FitsItsFixedPages_AndPlacesTheSignaturePageAt5(bool worstCase)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var data = Data(worstCase);

        var doc = LoanAgreementPdf.Compose(data);
        var images = doc.GenerateImages(new QuestPDF.Infrastructure.ImageGenerationSettings { ImageFormat = QuestPDF.Infrastructure.ImageFormat.Png }).ToList();

        // 5 fixed pages + Schedule A (1-2 pages) + Schedule B (48 rows).
        Assert.True(images.Count >= 7, $"pages: {images.Count}");

        // Review aid: set AGREEMENT_PNG_DIR to also write every page out as a PNG.
        if (Environment.GetEnvironmentVariable("AGREEMENT_PNG_DIR") is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
            for (var i = 0; i < images.Count; i++)
            {
                File.WriteAllBytes(Path.Combine(dir, $"agreement-{(worstCase ? "worst" : "normal")}-p{i + 1}.png"), images[i]);
            }
        }
    }

    [Fact]
    public void Agreement_BuildsAPdf_WithOnlyTheApplicant()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var pdf = LoanAgreementPdf.Build(Data(false, 1));

        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }
}

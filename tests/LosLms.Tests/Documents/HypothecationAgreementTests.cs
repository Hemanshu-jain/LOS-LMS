using LosLms.Models;
using LosLms.Services;
using LosLms.Services.Agreements;
using QuestPDF.Fluent;
using Xunit;

namespace LosLms.Tests;

public sealed class HypothecationAgreementTests
{
    private static AgreementData Data(bool worstCase = false, int parties = 3)
    {
        var d = LoanAgreementTests.Data(worstCase, parties);
        d.Lender.AgreementTemplate = AgreementTemplates.Hypothecation;
        d.Lender.Arbitrator1 = "Shri A. B. Deshmukh, Advocate, 12 Court Road, Nashik";
        d.Lender.Arbitrator2 = "Shri C. D. Patil, Retired District Judge, 4 Civil Lines, Nashik";
        d.Lender.ArbitrationFee = 5000m;
        return d with { LenderSignature = null };
    }

    [Fact]
    public void Form_HasAll22PagesInOrder_WithPrintedNumbersOnTheAgreementPages()
    {
        var pages = HypothecationForm.Pages;

        Assert.Equal(Enumerable.Range(1, 22), pages.Select(p => p.Number));
        Assert.Equal(Enumerable.Range(1, 10), pages.Where(p => p.Printed is not null).Select(p => p.Printed!.Value));
        Assert.Equal(3, pages.First(p => p.Printed == 1).Number);
        Assert.Contains("HYPOTHECATION CUM LOAN AGREEMENT", pages[2].Body[0].Text);
        Assert.Contains("ARTICLE 27 - ACCEPTANCE", pages[11].Body.Select(l => l.Text));
        Assert.Equal("arbitration", pages[20].Flag);
    }

    [Fact]
    public void Values_FillFromTheApplication_AndLeaveUnknownsOut()
    {
        var v = HypothecationValues.Build(Data());

        Assert.Equal("31", v["day"]);
        Assert.Equal("October", v["month"]);
        Assert.Equal("26", v["yy"]);
        Assert.Equal("Kishore Finance Pvt Ltd", v["lender_name"]);
        Assert.Equal("Tata", v["make"]);
        Assert.Equal("Ace Gold", v["model"]);
        Assert.Equal("MH15AB1234", v["reg_no"]);
        Assert.Equal("MAT123456789", v["chassis_no"]);
        Assert.Equal("14,00,000", v["loan_amount"]);
        Assert.Equal("Fourteen Lakh Only", v["amount_words"]);
        Assert.Equal("48", v["instalments"]);
        Assert.Equal("5,000", v["arbitration_fee"]);
        Assert.StartsWith("Shri A. B. Deshmukh", v["arbitrator_1"]);
        Assert.Equal("XXXXX1234X", v["pan"]);
        Assert.Equal("XXXX XXXX 5094", v["aadhaar"]);

        // not held by the app: stay blank
        Assert.DoesNotContain("driving_license", v.Keys);
        Assert.DoesNotContain("native_address", v.Keys);
        Assert.DoesNotContain("penal_rate_per_100", v.Keys);
        Assert.DoesNotContain("hp_cc", v.Keys);
        Assert.DoesNotContain("vehicle_verified_by", v.Keys);
    }

    [Fact]
    public void Values_BlankArbitratorsStayBlank()
    {
        var d = Data();
        d.Lender.Arbitrator1 = null;
        d.Lender.ArbitrationFee = null;

        var v = HypothecationValues.Build(d);

        Assert.DoesNotContain("arbitrator_1", v.Keys);
        Assert.DoesNotContain("arbitration_fee", v.Keys);
        Assert.DoesNotContain("arbitration_fee_words", v.Keys);
    }

    [Theory]
    [InlineData(0, "Zero Only")]
    [InlineData(7, "Seven Only")]
    [InlineData(1_400_000, "Fourteen Lakh Only")]
    [InlineData(1_425_350, "Fourteen Lakh Twenty Five Thousand Three Hundred Fifty Only")]
    [InlineData(25_000_000, "Two Crore Fifty Lakh Only")]
    public void NumberWords_UseTheIndianSystem(decimal amount, string expected) => Assert.Equal(expected, NumberWords.Rupees(amount));

    [Fact]
    public void Boxes_AreOnTheFormsSigningSpots_AndTheTemplateSwitchChoosesThem()
    {
        var boxes = new LoanAgreementSignatureBoxes();

        var std = boxes.BoxesFor("Applicant", AgreementTemplates.Standard)!;
        var hyp = boxes.BoxesFor("Applicant", AgreementTemplates.Hypothecation)!;
        var blank = boxes.BoxesFor("Applicant", null)!;

        Assert.Single(std);
        Assert.Equal(AgreementLayout.SignaturePage, std[0].Page);
        Assert.Equal(std, blank);
        Assert.True(hyp.Count > 12);
        Assert.Contains(hyp, b => b.Page == 3);
        Assert.Contains(hyp, b => b.Page == 16);
        Assert.Contains(hyp, b => b.Page == 18);
        Assert.Contains(hyp, b => b.Page == 21);
        Assert.DoesNotContain(hyp, b => b.Page == 15);
        Assert.All(hyp, b => { Assert.InRange(b.Page, 1, 22); Assert.InRange(b.Llx, 0, HypothecationLayout.PageW); Assert.InRange(b.Ury, 20, HypothecationLayout.PageH); Assert.True(b.Urx > b.Llx && b.Ury > b.Lly); });

        var guarantor = boxes.BoxesFor("Guarantor", AgreementTemplates.Hypothecation)!;
        Assert.Equal(new[] { 16, 20, 21 }, guarantor.Select(b => b.Page).ToArray());

        // a co-applicant signs beside the applicant, never on top of them
        var co = boxes.BoxesFor("CoApplicant", AgreementTemplates.Hypothecation)!;
        Assert.All(hyp.Join(co, a => a.Page, c => c.Page, (a, c) => (a, c)), x => Assert.True(x.c.Urx <= x.a.Llx || x.c.Llx >= x.a.Urx));
    }

    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 3)]
    [InlineData(false, 1)]
    public void Agreement_KeepsEveryFormPageOnOneSheet_AndAppendsTheScheduleAB(bool worstCase, int parties)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var d = Data(worstCase, parties);

        var images = HypothecationAgreementPdf.Compose(d)
            .GenerateImages(new QuestPDF.Infrastructure.ImageGenerationSettings { ImageFormat = QuestPDF.Infrastructure.ImageFormat.Png }).ToList();

        Assert.True(images.Count >= 24, $"pages: {images.Count}");   // 22 form pages + Schedule A + Schedule B (48 rows)

        if (Environment.GetEnvironmentVariable("HYP_PNG_DIR") is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
            for (var i = 0; i < images.Count; i++)
            {
                File.WriteAllBytes(Path.Combine(dir, $"hyp-{(worstCase ? "worst" : "normal")}-{parties}-p{i + 1:00}.png"), images[i]);
            }
        }
    }

    [Fact]
    public void ProposalForm_PlacesThePartyPhotoWhenWeHaveOne()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        var without = Data(false, 3);
        var with = without with { PartyPhotos = new Dictionary<string, byte[]> { ["Applicant"] = png, ["Guarantor"] = png } };

        var plain = HypothecationAgreementPdf.Build(without);
        var photographed = HypothecationAgreementPdf.Build(with);

        Assert.True(photographed.Length > plain.Length, "the photo is embedded");
    }

    [Fact]
    public void Dispatcher_BuildsTheTemplateTheCompanyChose()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var hyp = Data();
        var std = Data() with { };
        std.Lender.AgreementTemplate = AgreementTemplates.Standard;

        var hypPdf = AgreementBuilder.Build(hyp);
        var stdPdf = AgreementBuilder.Build(std);

        Assert.NotEqual(hypPdf.Length, stdPdf.Length);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(hypPdf, 0, 4));
    }

    [Fact]
    public void Template_UnknownOrBlankMeansStandard()
    {
        Assert.Equal(AgreementTemplates.Standard, AgreementTemplates.Normalize(null));
        Assert.Equal(AgreementTemplates.Standard, AgreementTemplates.Normalize("Something else"));
        Assert.Equal(AgreementTemplates.Hypothecation, AgreementTemplates.Normalize("Hypothecation"));
    }
}

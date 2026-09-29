using LosLms.Models;

namespace LosLms.Services;

/// <summary>
/// The Digio rate card (quote of 25 Aug 2026, ex-GST, ₹ per successful call). Seeds <see cref="ApiRate"/>;
/// call sites reference the constants so a typo cannot silently bill nothing.
/// </summary>
/// <remarks>
/// DigiCollect is slabbed by monthly volume — only the first slab (up to 5,000/month) is seeded, which is
/// far above any single NBFC's volume today. Account Aggregator and DigiDoc template plans are not
/// seeded (not onboarded); fixed fees (onboarding, credit purchase) are collected up front, not metered.
/// </remarks>
public static class DigioRates
{
    public const string ESignAadhaar = "ESIGN_AADHAAR";
    public const string DigiLockerFetch = "KYC_DIGILOCKER";
    public const string AadhaarOfflineXml = "KYC_AADHAAR_XML";
    public const string AadhaarMasking = "KYC_AADHAAR_MASK";
    public const string UpiVpaVerify = "KYC_UPI_VPA";
    public const string IdOcr = "KYC_ID_OCR";
    public const string IdVerify = "KYC_ID_VERIFY";
    public const string SelfieLiveness = "KYC_SELFIE";
    public const string FaceMatch = "KYC_FACE_MATCH";
    public const string FuzzyMatch = "KYC_FUZZY_MATCH";
    public const string BusinessKyc = "KYC_BUSINESS";
    public const string UanVerify = "KYC_UAN";
    public const string VideoKycOneWay = "KYC_VIDEO_1WAY";
    public const string Geolocation = "KYC_GEOLOCATION";
    public const string KraCheck = "KRA_CHECK";
    public const string KraFetch = "KRA_FETCH";
    public const string KraDownload = "KRA_DOWNLOAD";
    public const string KraSubmit = "KRA_SUBMIT";
    public const string KraModify = "KRA_MODIFY";
    public const string PennyDropCheque = "BANK_PENNY_CHEQUE";
    public const string PennyDrop = "BANK_PENNY_DROP";
    public const string ReversePennyDrop = "BANK_PENNY_REVERSE";
    public const string Pennyless = "BANK_PENNYLESS";
    public const string BankStatementAnalyzer = "BANK_STMT_ANALYZER";
    public const string MandateApi = "MANDATE_API";
    public const string MandatePhysical = "MANDATE_PHYSICAL";
    public const string MandateEsign = "MANDATE_ESIGN";
    public const string MandateDebit = "MANDATE_DEBIT";

    public static readonly IReadOnlyList<ApiRate> Seed = new ApiRate[]
    {
        R(ESignAadhaar, "Aadhaar e-Sign (per credit)", "DigiSign", 10.60m),
        R(DigiLockerFetch, "DigiLocker document fetch", "DigiKYC", 2.00m),
        R(AadhaarOfflineXml, "Aadhaar Offline XML KYC", "DigiKYC", 2.00m),
        R(AadhaarMasking, "Aadhaar masking", "DigiKYC", 1.00m),
        R(UpiVpaVerify, "UPI VPA verification", "DigiKYC", 1.00m),
        R(IdOcr, "ID card OCR (Aadhaar/PAN/Voter/Passport/RC)", "DigiKYC", 2.00m),
        R(IdVerify, "ID verification (PAN, Voter ID…)", "DigiKYC", 1.50m),
        R(SelfieLiveness, "Selfie with liveness + geotag", "DigiKYC", 2.50m),
        R(FaceMatch, "Face match", "DigiKYC", 1.00m),
        R(FuzzyMatch, "Name/address fuzzy match", "DigiKYC", 1.00m),
        R(BusinessKyc, "Business KYC (GST, PAN, DIN, CIN…)", "DigiKYC", 3.00m),
        R(UanVerify, "UAN verification", "DigiKYC", 15.00m),
        R(VideoKycOneWay, "1-way Video KYC", "DigiKYC", 6.00m),
        R(Geolocation, "Geolocation add-on", "DigiKYC", 0.50m),
        R(KraCheck, "KRA check", "DigiKYC", 1.00m),
        R(KraFetch, "KRA detail fetch", "DigiKYC", 1.00m),
        R(KraDownload, "KRA document download", "DigiKYC", 1.00m),
        R(KraSubmit, "KRA submission", "DigiKYC", 1.00m),
        R(KraModify, "KRA modify", "DigiKYC", 1.00m),
        R(PennyDropCheque, "Cheque + penny drop", "Bank verification", 3.50m),
        R(PennyDrop, "Penny drop (independent)", "Bank verification", 2.50m),
        R(ReversePennyDrop, "Reverse penny drop", "Bank verification", 2.00m),
        R(Pennyless, "Pennyless check", "Bank verification", 2.00m),
        R(BankStatementAnalyzer, "Bank statement analyzer (per statement)", "Bank verification", 13.00m),
        R(MandateApi, "e-Mandate (netbanking/debit card/Aadhaar)", "DigiCollect", 4.50m),
        R(MandatePhysical, "Physical / scan NACH mandate", "DigiCollect", 5.00m),
        R(MandateEsign, "eSign eNACH mandate", "DigiCollect", 12.00m),
        R(MandateDebit, "ACH debit presentation", "DigiCollect", 2.00m),
    };

    private static ApiRate R(string code, string name, string product, decimal rate) =>
        new() { Code = code, Name = name, Product = product, UnitRate = rate, IsActive = true };
}

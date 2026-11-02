namespace LosLms.Services;

/// <summary>
/// The loan-agreement e-Sign flow lives in <see cref="DigioEsignService"/>. Nothing sets the agreement Signed except Digio's
/// verified webhook / confirm (<see cref="DigioWebhookService"/>), which is also when the e-Sign credit is metered.
/// </summary>
public static class EsignService
{
    public const string Unavailable = "E-Signature dispatch unavailable — Digio e-Sign is not switched on for this company.";
}

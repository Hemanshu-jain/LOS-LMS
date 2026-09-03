namespace LosLms.Services;

/// <summary>Outcome of dispatching an agreement to an e-signature provider.</summary>
/// <param name="IsConfigured">False whenever no provider is wired — the only state reachable here.</param>
public sealed record EsignDispatchResult(bool IsConfigured)
{
    public static readonly EsignDispatchResult NotConfigured = new(false);
}

/// <summary>
/// Dispatching a generated agreement for e-signature. An honest stub — no provider is configured.
/// </summary>
/// <remarks>
/// The agreement PDF itself is generated for real (<see cref="LoanAgreementPdf"/>). Only the dispatch
/// to an e-sign vendor is stubbed: <see cref="DispatchAsync"/> reports "not configured" and the
/// disbursement's <c>AgreementEsignStatus</c> stays <c>NotSent</c>.
///
/// This one carries real legal weight, so the discipline is absolute: nothing in this build may ever
/// set the status to <c>Signed</c>. Only a verified webhook from a real provider is allowed to, and
/// that path does not exist yet.
/// </remarks>
public static class EsignService
{
    public const string Unavailable = "E-Signature dispatch unavailable — provider not configured.";

    /// <summary>
    /// Attempts to dispatch the agreement for signature. Always "not configured" in this build.
    /// </summary>
    public static Task<EsignDispatchResult> DispatchAsync(string? agreementFilePath)
    {
        // REAL PROVIDER: upload the agreement PDF to the e-sign vendor, create a signature request for
        // the signatories, and return new EsignDispatchResult(IsConfigured: true) once accepted. The
        // vendor's signature-completion WEBHOOK — verified — is the only thing allowed to move
        // AgreementEsignStatus to 'Signed'. Never set 'Signed' from here or optimistically in the UI.
        return Task.FromResult(EsignDispatchResult.NotConfigured);
    }
}

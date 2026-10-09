namespace LosLms.Services;

/// <summary>
/// Sends a generated agreement to Digio for Aadhaar e-Sign (POST /v2/client/document/uploadpdf, base64 PDF).
/// </summary>
/// <remarks>
/// This carries legal weight, so the discipline is absolute: nothing here sets the agreement 'Signed'.
/// Digio's verified DOC.SIGNED webhook is the only thing allowed to (<see cref="DigioClient.ApplyWebhookAsync"/>),
/// and that is also when the e-Sign credit is metered — not on upload.
/// </remarks>
public static class EsignService
{
    public const string Unavailable = "E-Signature dispatch unavailable — Digio is not configured for this company.";

    /// <param name="signerIdentifier">Applicant's mobile or email — Digio sends the signing link there.</param>
    public static async Task<DigioResult> DispatchAsync(
        DigioClient digio, int companyId, string applicationId, string agreementFullPath,
        string signerIdentifier, string? signerName)
    {
        if (digio.Credentials(companyId) is null)
        {
            return DigioResult.NotConfigured; // don't read the PDF for nothing
        }

        var body = new
        {
            file_name = Path.GetFileName(agreementFullPath),
            file_data = Convert.ToBase64String(await File.ReadAllBytesAsync(agreementFullPath)),
            signers = new[]
            {
                new { identifier = signerIdentifier, name = signerName, reason = "Loan agreement", sign_type = "aadhaar" },
            },
            expire_in_days = 10,
            notify_signers = true,
            send_sign_link = true,
        };

        return await digio.PostAsync(companyId, apiCode: null, "/v2/client/document/uploadpdf", body, applicationId);
    }
}

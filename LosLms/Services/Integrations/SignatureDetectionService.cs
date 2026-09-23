using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace LosLms.Services;

/// <summary>Where a signature check ended up.</summary>
public enum SignatureStatus
{
    /// <summary>No Gemini key configured — the staff tick stays the only source of truth.</summary>
    NotConfigured,
    Signed,
    NotSigned,
    /// <summary>The model answered but was not confident enough to assert either way.</summary>
    Uncertain,
    /// <summary>The call or its parsing failed — staff should tick manually.</summary>
    Error,
}

/// <param name="Status">The outcome.</param>
/// <param name="Confidence">0–1 when the model returned one; null otherwise.</param>
/// <param name="Message">A short, safe message for the screen. Never a raw provider error/stack.</param>
public sealed record SignatureDetectionResult(SignatureStatus Status, double? Confidence, string? Message)
{
    public static readonly SignatureDetectionResult NotConfigured =
        new(SignatureStatus.NotConfigured, null, "Signature auto-check unavailable — Gemini key not configured.");
}

/// <summary>
/// Asks Gemini whether a scanned document image/PDF contains a signature, to pre-fill the per-document
/// "Signed" tick on the Document Checklist (item #6). Honest by construction: with no key it returns
/// <see cref="SignatureDetectionResult.NotConfigured"/> and never guesses, exactly like
/// <see cref="DigiKycService"/>. The tick a human sets is always authoritative; this only offers a
/// suggested value the officer can accept or override.
/// </summary>
public sealed class SignatureDetectionService
{
    private const double AssertThreshold = 0.6;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GeminiOptions _options;
    private readonly ILogger<SignatureDetectionService> _logger;

    public SignatureDetectionService(
        IHttpClientFactory httpClientFactory,
        IOptions<GeminiOptions> options,
        ILogger<SignatureDetectionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    /// <summary>
    /// Returns whether the document appears signed. Supports image/* and application/pdf, the only types
    /// the upload allows. Any failure is swallowed into an Error result so the screen degrades to the
    /// manual tick rather than throwing.
    /// </summary>
    public async Task<SignatureDetectionResult> DetectAsync(byte[] fileBytes, string mimeType, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            return SignatureDetectionResult.NotConfigured;
        }

        if (fileBytes.Length == 0)
        {
            return new SignatureDetectionResult(SignatureStatus.Error, null, "The document is empty.");
        }

        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text =
                                "You are verifying loan KYC documents. Look at the attached document and decide " +
                                "whether it carries a signature (a handwritten signed mark, initials, or a digital " +
                                "signature block by a person). Reply with ONLY compact JSON and nothing else: " +
                                "{\"signed\": true or false, \"confidence\": a number from 0 to 1}." },
                            new { inline_data = new { mime_type = mimeType, data = Convert.ToBase64String(fileBytes) } },
                        },
                    },
                },
                generationConfig = new { temperature = 0, responseMimeType = "application/json" },
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";
            using var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient();
            using var response = await client.PostAsync(url, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini signature check returned {Status}.", (int)response.StatusCode);
                return new SignatureDetectionResult(SignatureStatus.Error, null, "Signature check failed — try again or tick manually.");
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            return Parse(payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini signature check failed.");
            return new SignatureDetectionResult(SignatureStatus.Error, null, "Signature check failed — try again or tick manually.");
        }
    }

    /// <summary>Pulls the model's text out of the Gemini envelope, then the {signed, confidence} out of that.</summary>
    private static SignatureDetectionResult Parse(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                return new SignatureDetectionResult(SignatureStatus.Error, null, "Signature check returned nothing.");
            }

            // The model is asked for pure JSON, but guard against a stray ```json fence just in case.
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return new SignatureDetectionResult(SignatureStatus.Uncertain, null, "Signature check was inconclusive.");
            }

            using var answer = JsonDocument.Parse(text.Substring(start, end - start + 1));
            var root = answer.RootElement;
            var signed = root.TryGetProperty("signed", out var s) && s.ValueKind == JsonValueKind.True;
            double? confidence = root.TryGetProperty("confidence", out var c) && c.TryGetDouble(out var cv) ? cv : null;

            if (confidence is { } value && value < AssertThreshold)
            {
                return new SignatureDetectionResult(SignatureStatus.Uncertain, confidence,
                    $"Signature unclear ({value:P0}) — please verify and tick manually.");
            }

            return signed
                ? new SignatureDetectionResult(SignatureStatus.Signed, confidence, "Signature detected.")
                : new SignatureDetectionResult(SignatureStatus.NotSigned, confidence, "No signature detected — please verify.");
        }
        catch (Exception)
        {
            return new SignatureDetectionResult(SignatureStatus.Error, null, "Could not read the signature-check result.");
        }
    }
}

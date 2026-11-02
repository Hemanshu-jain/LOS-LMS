namespace LosLms.Services;

/// <summary>
/// Configuration for the Gemini vision call that detects whether a scanned document carries a
/// signature (item #6). Bound from the <c>Gemini</c> section. <see cref="ApiKey"/> is blank until the
/// client provides one, at which point the feature turns itself on — no code change. Keep the real key
/// out of appsettings; set it via environment variable <c>Gemini__ApiKey</c> or user-secrets.
/// </summary>
public sealed class GeminiOptions
{
    public const string Section = "Gemini";

    public string? ApiKey { get; set; }

    /// <summary>The vision-capable model to call. Configurable so a model rename needs no rebuild.</summary>
    public string Model { get; set; } = "gemini-2.5-flash";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}

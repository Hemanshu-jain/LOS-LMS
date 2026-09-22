namespace LosLms.Services;

/// <summary>
/// Confirms an uploaded file's bytes actually match its claimed extension. The upload pages already
/// whitelist the extension and the browser-declared content-type, but both are trivially forged — a
/// renamed executable sails through an extension check. Sniffing the leading "magic" bytes closes that:
/// a file named <c>.pdf</c> that is not really a PDF is rejected before it is written to disk.
/// </summary>
public static class FileSignature
{
    /// <summary>
    /// True when <paramref name="content"/> begins with the signature expected for
    /// <paramref name="extension"/>. Only the extensions the upload pages allow (pdf/jpg/jpeg/png/webp)
    /// are recognised; anything else returns false, since the caller has already whitelisted the set.
    /// </summary>
    public static bool Matches(string extension, byte[] content) => extension.ToLowerInvariant() switch
    {
        ".pdf" => StartsWith(content, 0x25, 0x50, 0x44, 0x46),                          // %PDF
        ".jpg" or ".jpeg" => StartsWith(content, 0xFF, 0xD8, 0xFF),                     // JPEG SOI
        ".png" => StartsWith(content, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),  // PNG
        ".webp" => content.Length >= 12
            && StartsWith(content, 0x52, 0x49, 0x46, 0x46)                              // RIFF
            && content[8] == 0x57 && content[9] == 0x45 && content[10] == 0x42 && content[11] == 0x50, // WEBP
        ".webm" => StartsWith(content, 0x1A, 0x45, 0xDF, 0xA3),                         // EBML (Matroska/WebM)
        _ => false,
    };

    private static bool StartsWith(byte[] content, params byte[] prefix)
    {
        if (content.Length < prefix.Length)
        {
            return false;
        }

        for (var i = 0; i < prefix.Length; i++)
        {
            if (content[i] != prefix[i])
            {
                return false;
            }
        }

        return true;
    }
}

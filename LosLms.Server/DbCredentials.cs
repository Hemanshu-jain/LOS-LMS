using System.Security.Cryptography;

namespace LosLms.Server;

/// <summary>
/// The bundled MySQL's generated credential. Created once on first run (the operator is never asked to
/// choose or type anything) and persisted next to the exe so every later run reuses it. The app — and
/// the launcher's own graceful-shutdown call — always connect as <c>los</c>, so that is the only
/// secret worth keeping. (root is unreachable over TCP with skip-name-resolve and no named pipe, so no
/// root password is stored.)
/// </summary>
internal sealed record DbCredentials(string AppPassword)
{
    public const string Database = "los_lms";
    public const string AppUser = "los";

    public static DbCredentials Generate() => new(RandomToken());

    // AllowPublicKeyRetrieval is needed because MySQL 8.0 defaults to caching_sha2_password: on a
    // localhost connection without TLS the client must fetch the server's RSA public key to complete
    // auth. TreatTinyAsBoolean is deliberately left at its default — Pomelo relies on it for bool
    // mapping, so it must not be overridden here.
    public string AppConnectionString(int port) =>
        $"Server=127.0.0.1;Port={port};Database={Database};User Id={AppUser};Password={AppPassword};" +
        "AllowPublicKeyRetrieval=True;";

    // One secret, one line — no JSON envelope for a single string.
    public void Save() => File.WriteAllText(Paths.CredentialsFile, AppPassword);

    public static DbCredentials? TryLoad()
    {
        try
        {
            return File.Exists(Paths.CredentialsFile)
                ? new DbCredentials(File.ReadAllText(Paths.CredentialsFile).Trim())
                : null;
        }
        catch (Exception ex)
        {
            Log.Error("Could not read the stored database credentials", ex);
            return null;
        }
    }

    // Hex is already connection-string- and shell-safe (no +/=), so no character replacement is needed.
    private static string RandomToken()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }
}

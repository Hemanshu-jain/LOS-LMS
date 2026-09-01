namespace LosLms.Server;

/// <summary>
/// Remembers, per device, whether this machine is the host or a staff client. Stored under
/// %LOCALAPPDATA% rather than next to the exe so the extracted app folder stays clean and the choice
/// survives even if the folder is moved. To change a machine's role, delete this file (documented in
/// the read-me).
/// </summary>
internal static class RoleStore
{
    public const string Host = "host";
    public const string Client = "client";

    private static readonly string RoleFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LOS-LMS", "role.txt");

    /// <summary>The stored role, or null if this device has never chosen one.</summary>
    public static string? Get()
    {
        try
        {
            if (File.Exists(RoleFile))
            {
                var value = File.ReadAllText(RoleFile).Trim();
                if (value is Host or Client)
                {
                    return value;
                }
            }
        }
        catch
        {
            // Treat an unreadable marker as "not chosen" — the picker will run.
        }

        return null;
    }

    public static void Set(string role)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RoleFile)!);
            File.WriteAllText(RoleFile, role);
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not save the device role ({ex.Message}); it will be asked again next launch.");
        }
    }
}

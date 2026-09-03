using System.Reflection;
using System.Text;

namespace LosLms.Data;

/// <summary>
/// The vendor/developer break-glass SuperAdmin — a fixed login the operator (you) can use on any
/// deployment to recover access when a client loses their own admin/superadmin passwords.
///
/// The password is NOT in source. It is baked at build time from an untracked <c>master-key.txt</c>
/// (see publish.ps1), so it never reaches the public repo, and a build made from the public source
/// alone carries no master account at all. When no password is baked, <see cref="Password"/> is null
/// and <see cref="IdentitySeeder"/> creates no master account. (A determined party can still extract
/// the string from a shipped binary — a known, accepted trade-off; the strong password is what defends
/// the realistic attack, a login attempt over the tunnel.)
/// </summary>
internal static class MasterAccount
{
    public const string Id = "usr-master";
    public const string Email = "admin@loslms.com";
    public const string DisplayName = "Master Administrator";

    /// <summary>The baked master password, or null when none was baked at build time.</summary>
    public static string? Password { get; } = ReadBakedPassword();

    private static string? ReadBakedPassword()
    {
        // Stored base64 (set by publish.ps1) so any character survives the build command and the
        // assembly metadata unharmed.
        var encoded = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "MasterAdminPassword")?.Value;

        if (string.IsNullOrEmpty(encoded))
        {
            return null;
        }

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encoded)).Trim();
        }
        catch
        {
            return null;
        }
    }
}

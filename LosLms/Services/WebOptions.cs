namespace LosLms.Services;

/// <summary>
/// Settings that only apply to the public multi-tenant web instance (los-lms.bhodhix.com), bound from
/// the <c>Web</c> configuration section. Absent on the desktop client build — the section simply is not
/// present in its config — so every flag here defaults off and the desktop app behaves exactly as before.
/// </summary>
public sealed class WebOptions
{
    public const string Section = "Web";

    /// <summary>
    /// When true, the anonymous <c>/account/register</c> page is open so anyone can create their own
    /// company and run it like a normal tenant. Off by default: on the desktop, single-tenant build the
    /// register page is closed and the sign-in "Create your company" link is hidden.
    /// </summary>
    public bool AllowSelfRegistration { get; set; }
}

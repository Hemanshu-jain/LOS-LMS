using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace LosLms.Services;

/// <summary>
/// Who is signed in and which company they belong to. Scoped, so in Blazor Server there is one per
/// circuit; <see cref="LosDbContext"/> snapshots it at construction and its global query filters key
/// off the result.
/// </summary>
/// <remarks>
/// The company is read from the signed authentication cookie's claims and from nowhere else — never
/// from a route parameter, query string, form field or header, all of which the browser can edit.
/// <see cref="AppUserClaimsPrincipalFactory"/> is what puts the claim there at sign-in.
///
/// Deliberately NOT sourced from <c>IHttpContextAccessor</c>: inside a SignalR circuit there is no
/// live HttpContext, so that would read stale or null on every interactive page.
///
/// Fails closed. An unresolved user has a null <see cref="CompanyId"/>, and
/// <c>Application.CompanyId</c> is non-nullable, so the filter matches no rows at all rather than
/// falling back to "show everything".
/// </remarks>
public sealed class TenantContext
{
    /// <summary>Claim type carrying the user's company id.</summary>
    public const string CompanyIdClaim = "los:company_id";

    /// <summary>Claim type carrying the user's display name. A separate claim rather than an override
    /// of <see cref="ClaimTypes.Name"/>, which Identity uses for the username and relies on.</summary>
    public const string DisplayNameClaim = "los:display_name";

    /// <summary>Present only while the user is still on a temporary password.</summary>
    public const string MustChangePasswordClaim = "los:must_change_password";

    public const string SuperAdminRole = "SuperAdmin";
    public const string AdminRole = "Admin";
    public const string StaffRole = "Staff";

    /// <summary>
    /// The two roles allowed to administer a company. A const because <c>[Authorize(Roles = …)]</c>
    /// needs a compile-time constant, and spelling the pair out at each page is how they drift apart.
    /// </summary>
    public const string AdminRoles = AdminRole + "," + SuperAdminRole;

    private readonly AuthenticationStateProvider? _authProvider;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private bool _loaded;

    public TenantContext(AuthenticationStateProvider authProvider, IHttpContextAccessor httpContextAccessor)
    {
        _authProvider = authProvider;
        _httpContextAccessor = httpContextAccessor;

        // Signing in or out inside a live circuit must not leave the previous user's company behind.
        authProvider.AuthenticationStateChanged += _ => _loaded = false;
    }

    private TenantContext()
    {
    }

    /// <summary>Owning company, or null for a SuperAdmin and for anyone not signed in.</summary>
    public int? CompanyId { get; private set; }

    /// <summary>True when the signed-in user holds the SuperAdmin role and so is not company-scoped.</summary>
    public bool IsSuperAdmin { get; private set; }

    /// <summary>True for a company Admin. Does NOT include SuperAdmin — check both where either will do.</summary>
    public bool IsAdmin { get; private set; }

    /// <summary>True when someone is actually signed in. Distinguishes a SuperAdmin from an anonymous
    /// request — both have a null <see cref="CompanyId"/>, but they must not be treated alike.</summary>
    public bool HasUser { get; private set; }

    public string? UserId { get; private set; }

    public string? DisplayName { get; private set; }

    /// <summary>
    /// Resolves the claims once per circuit. Called by <see cref="TenantDbContextFactory"/> before
    /// every context it hands out, which is why nothing here has to block on an async call.
    /// </summary>
    public async Task EnsureLoadedAsync()
    {
        if (_loaded || _authProvider is null)
        {
            return;
        }

        // Order matters, and not for preference — for correctness.
        //
        // A plain HTTP request (the /files endpoint, the statically rendered /account pages) has a
        // real HttpContext whose User is authoritative. It has no Blazor circuit, and
        // ServerAuthenticationStateProvider THROWS outright when called from outside a component's
        // DI scope, so it cannot be the first choice.
        //
        // A live interactive circuit is the reverse: the original request finished long ago, so
        // HttpContext is null and the state provider is the only real source. Trying HttpContext
        // first and falling through covers both without either one guessing about the other.
        if (_httpContextAccessor?.HttpContext?.User is { } httpUser)
        {
            Apply(httpUser);
            return;
        }

        var state = await _authProvider.GetAuthenticationStateAsync();
        Apply(state.User);
    }

    private void Apply(ClaimsPrincipal user)
    {
        HasUser = user.Identity?.IsAuthenticated == true;
        IsSuperAdmin = HasUser && user.IsInRole(SuperAdminRole);
        IsAdmin = HasUser && user.IsInRole(AdminRole);
        UserId = HasUser ? user.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        DisplayName = HasUser ? user.FindFirstValue(DisplayNameClaim) : null;

        CompanyId = HasUser && int.TryParse(user.FindFirstValue(CompanyIdClaim), out var companyId)
            ? companyId
            : null;

        _loaded = true;
    }

    /// <summary>
    /// An unscoped context for startup seeding, which runs before any request and therefore has no
    /// user to read. Internal and used in exactly one place — <c>IdentitySeeder</c>. It is not a
    /// general escape hatch: application code gets its TenantContext from DI and cannot construct one.
    /// </summary>
    internal static TenantContext ForSeeding() => new()
    {
        _loaded = true,
        HasUser = true,
        IsSuperAdmin = true,
    };
}

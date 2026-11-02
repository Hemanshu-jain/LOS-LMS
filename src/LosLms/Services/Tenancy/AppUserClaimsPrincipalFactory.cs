using System.Security.Claims;
using LosLms.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LosLms.Services;

/// <summary>
/// Adds the company id and display name to the authentication cookie at sign-in.
/// </summary>
/// <remarks>
/// Putting the company in a claim rather than looking it up per request is what lets
/// <see cref="TenantContext"/> stay synchronous and free of database access — which matters, because
/// the thing it scopes IS the database. A claim also cannot be tampered with: the cookie is signed
/// by the data-protection key, so a user cannot edit their way into another company.
///
/// The trade-off is staleness. Moving a user between companies, deactivating them, or changing their
/// display name only takes effect on their next sign-in. That is acceptable while there is no admin
/// UI to do any of those things; when there is, either add a security stamp revalidation interval or
/// call <c>RefreshSignInAsync</c> after the change.
/// </remarks>
public sealed class AppUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.CompanyId is { } companyId)
        {
            identity.AddClaim(new Claim(TenantContext.CompanyIdClaim, companyId.ToString()));
        }

        identity.AddClaim(new Claim(TenantContext.DisplayNameClaim, user.DisplayName));

        // Carried in the cookie so the layout guard can enforce it without a database round-trip on
        // every render. ChangePassword calls RefreshSignInAsync, which reissues the cookie without it.
        if (user.MustChangePassword)
        {
            identity.AddClaim(new Claim(TenantContext.MustChangePasswordClaim, "true"));
        }

        return identity;
    }
}

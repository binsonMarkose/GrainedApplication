using System.Security.Claims;
using Grained.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Grained.Infrastructure.Identity;

// Adds a ChurchId claim to the sign-in principal so the app can enforce church-level
// data isolation without a database round-trip on every request.
public class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>(userManager, roleManager, options)
{
    public const string ChurchIdClaimType = "ChurchId";
    public const string FullNameClaimType = "FullName";

    public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var principal = await base.CreateAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;

        identity.AddClaim(new Claim(FullNameClaimType, user.FullName));
        if (user.ChurchId is not null)
        {
            identity.AddClaim(new Claim(ChurchIdClaimType, user.ChurchId.Value.ToString()));
        }

        return principal;
    }
}

using CondoSphere.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CondoSphere.Infrastructure
{
    public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, IdentityRole>
    {
        public AppClaimsPrincipalFactory(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> options)
            : base(userManager, roleManager, options) { }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
        {
            var id = await base.GenerateClaimsAsync(user);
            if (user.CompanyId.HasValue)
                id.AddClaim(new Claim("company_id", user.CompanyId.Value.ToString()));
            if (!string.IsNullOrWhiteSpace(user.FullName))
                id.AddClaim(new Claim(ClaimTypes.GivenName, user.FullName));
            return id;
        }
    }
}

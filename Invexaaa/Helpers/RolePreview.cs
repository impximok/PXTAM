// Helpers/RolePreview.cs
using System.Linq;
using System.Security.Claims;

namespace Invexaaa.Helpers
{
    public static class RolePreview
    {
        // Build a ClaimsPrincipal that is the same user, but with exactly one role claim = roleIdToPreview
        public static ClaimsPrincipal Build(ClaimsPrincipal original, string roleIdToPreview)
        {
            var identity = new ClaimsIdentity(original.Identity, original.Claims
                .Where(c => c.Type != ClaimTypes.Role)); // strip existing roles

            identity.AddClaim(new Claim(ClaimTypes.Role, roleIdToPreview)); // add preview role id
            return new ClaimsPrincipal(identity);
        }
    }
}

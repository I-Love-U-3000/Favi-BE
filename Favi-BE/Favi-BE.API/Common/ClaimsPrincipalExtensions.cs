using System.Security.Claims;

namespace Favi_BE.Common
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim))
                throw new UnauthorizedAccessException("Missing NameIdentifier claim.");

            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid user ID format.");

            return userId;
        }

        public static Guid? GetUserIdOrNull(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdClaim))
                return null;

            if (!Guid.TryParse(userIdClaim, out var userId))
                return null;

            return userId;
        }

        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            var role = user.FindFirstValue(ClaimTypes.Role);
            return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetUsername(this ClaimsPrincipal user)
        {
            var username = user.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(username))
                throw new UnauthorizedAccessException("Missing username claim.");

            return username;
        }
    }
}

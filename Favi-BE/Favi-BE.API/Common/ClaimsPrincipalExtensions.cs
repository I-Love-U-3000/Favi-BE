using System.Security.Claims;
using System.Text.Json;

namespace Favi_BE.Common
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserIdFromMetadata(this ClaimsPrincipal user)
        {
            // Lấy claim "user_metadata"
            var meta = user.FindFirstValue("user_metadata");
            if (string.IsNullOrWhiteSpace(meta))
                throw new UnauthorizedAccessException("Missing user_metadata claim.");

            try
            {
                using var doc = JsonDocument.Parse(meta);
                var root = doc.RootElement;
                var sub = root.GetProperty("sub").GetString();
                if (string.IsNullOrWhiteSpace(sub))
                    throw new UnauthorizedAccessException("Missing sub in user_metadata.");

                return Guid.Parse(sub);
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException($"Invalid user_metadata: {ex.Message}");
            }
        }

        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            var role = user.FindFirstValue("account_role");
            return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}

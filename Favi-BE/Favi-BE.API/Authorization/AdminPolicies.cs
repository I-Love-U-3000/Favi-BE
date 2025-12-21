using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace Favi_BE.Authorization;

public static class AdminPolicies
{
    public const string RequireAdmin = "RequireAdmin";
}

public sealed class RequireAdminRequirement : IAuthorizationRequirement;

public sealed class RequireAdminHandler : AuthorizationHandler<RequireAdminRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequireAdminRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        if (IsAdminFromClaim(context.User, "account_role"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Supabase may also embed roles inside app_metadata/user_metadata payloads.
        if (IsAdminFromJsonClaim(context.User, "app_metadata") ||
            IsAdminFromJsonClaim(context.User, "user_metadata"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool IsAdminFromClaim(ClaimsPrincipal user, string claimType)
    {
        var value = user.FindFirstValue(claimType);
        return string.Equals(value, "admin", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAdminFromJsonClaim(ClaimsPrincipal user, string claimType)
    {
        var json = user.FindFirstValue(claimType);
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("role", out var roleProp))
            {
                var role = roleProp.GetString();
                return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            // ignored – malformed metadata should not crash auth.
        }

        return false;
    }
}

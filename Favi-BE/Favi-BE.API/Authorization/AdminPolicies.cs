using System.Security.Claims;
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

        // Check for standard role claim
        var role = context.User.FindFirstValue(ClaimTypes.Role);
        if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

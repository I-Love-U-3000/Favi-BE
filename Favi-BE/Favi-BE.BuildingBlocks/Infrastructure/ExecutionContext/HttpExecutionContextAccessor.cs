using Favi_BE.BuildingBlocks.Application;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Favi_BE.BuildingBlocks.Infrastructure.ExecutionContext;

public sealed class HttpExecutionContextAccessor : IExecutionContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpExecutionContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }
                
            try
            {
                var sub = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(sub, out var userId) ? userId : null;
            }
            catch
            {
                return null;
            }
        }
    }

    public string? CorrelationId =>
        _httpContextAccessor.HttpContext?.TraceIdentifier;
}

using System.Net;
using System.Text.Json;
using Favi_BE.Common;
using StackExchange.Redis;

namespace Favi_BE.API.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;
    private readonly IConnectionMultiplexer _redis;

    private static readonly HashSet<string> ExcludedPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/chatHub",
        "/notificationHub",
        "/callHub",
        "/scalar",
        "/openapi",
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh-token",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/seed"
    };

    public IdempotencyMiddleware(
        RequestDelegate next,
        ILogger<IdempotencyMiddleware> logger,
        IConnectionMultiplexer redis)
    {
        _next = next;
        _logger = logger;
        _redis = redis;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method.ToUpperInvariant();
        var isMutating = method is "POST" or "PUT" or "DELETE" or "PATCH";

        if (!isMutating)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        foreach (var prefix in ExcludedPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var headerVal) || string.IsNullOrWhiteSpace(headerVal))
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                statusCode = 400,
                code = "MISSING_IDEMPOTENCY_KEY",
                message = "Header 'Idempotency-Key' là bắt buộc đối với yêu cầu thay đổi dữ liệu."
            }));
            return;
        }

        var idempotencyKey = headerVal.ToString().Trim();
        if (idempotencyKey.Length > 128)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                statusCode = 400,
                code = "INVALID_IDEMPOTENCY_KEY",
                message = "Header 'Idempotency-Key' không hợp lệ."
            }));
            return;
        }

        var userId = context.User.Identity?.IsAuthenticated == true
            ? context.User.GetUserId().ToString()
            : "anon";

        var cacheKey = $"favi:idempotency:{userId}:{idempotencyKey}";
        IDatabase? db = null;
        try
        {
            db = _redis.GetDatabase();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for Idempotency check. Proceeding without caching.");
        }

        if (db != null)
        {
            try
            {
                var cached = await db.StringGetAsync(cacheKey);
                if (!cached.IsNullOrEmpty)
                {
                    var cachedStr = cached.ToString();
                    if (cachedStr == "IN_FLIGHT")
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                        {
                            statusCode = 409,
                            code = "IDEMPOTENT_OPERATION_IN_PROGRESS",
                            message = "Yêu cầu trước đó đang được xử lý, vui lòng chờ."
                        }));
                        return;
                    }

                    var cachedResponse = JsonSerializer.Deserialize<CachedIdempotentResponse>(cachedStr);
                    if (cachedResponse != null)
                    {
                        context.Response.StatusCode = cachedResponse.StatusCode;
                        context.Response.ContentType = cachedResponse.ContentType ?? "application/json";
                        await context.Response.WriteAsync(cachedResponse.Body ?? string.Empty);
                        return;
                    }
                }

                // Acquire lock for in-flight operation (TTL 60s)
                var acquired = await db.StringSetAsync(cacheKey, "IN_FLIGHT", TimeSpan.FromSeconds(60), When.NotExists);
                if (!acquired)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        statusCode = 409,
                        code = "IDEMPOTENT_OPERATION_IN_PROGRESS",
                        message = "Yêu cầu trước đó đang được xử lý, vui lòng chờ."
                    }));
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while checking idempotency key in Redis.");
            }
        }

        // Intercept response to cache on 2xx
        var originalBodyStream = context.Response.Body;
        await using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await _next(context);

            memoryStream.Position = 0;
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
            memoryStream.Position = 0;

            if (db != null)
            {
                if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                {
                    var cacheData = new CachedIdempotentResponse(
                        context.Response.StatusCode,
                        context.Response.ContentType ?? "application/json",
                        responseBody
                    );
                    await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(cacheData), TimeSpan.FromMinutes(5));
                }
                else
                {
                    // Clean up lock if request failed so user can retry
                    await db.KeyDeleteAsync(cacheKey);
                }
            }

            await memoryStream.CopyToAsync(originalBodyStream);
        }
        catch (Exception)
        {
            if (db != null)
            {
                try { await db.KeyDeleteAsync(cacheKey); } catch { /* best effort */ }
            }
            throw;
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private sealed record CachedIdempotentResponse(int StatusCode, string? ContentType, string? Body);
}

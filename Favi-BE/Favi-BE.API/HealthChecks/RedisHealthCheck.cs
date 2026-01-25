using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using System.Diagnostics;

namespace Favi_BE.API.HealthChecks;

/// <summary>
/// Health check for Redis cache service
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(
        IConfiguration configuration,
        ILogger<RedisHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var connectionString = _configuration["Redis:ConnectionString"] ?? "redis:6379";

        try
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.ConnectTimeout = 5000;
            options.SyncTimeout = 5000;
            options.AbortOnConnectFail = false;

            using var connection = await ConnectionMultiplexer.ConnectAsync(options);
            var db = connection.GetDatabase();

            // Simple PING command to check connectivity
            var latency = await db.PingAsync();
            sw.Stop();

            var data = new Dictionary<string, object>
            {
                { "ResponseTimeMs", sw.ElapsedMilliseconds },
                { "PingLatencyMs", latency.TotalMilliseconds },
                { "Service", "Redis" },
                { "Endpoint", connectionString.Split(',')[0] }, // Hide password if any
                { "IsConnected", connection.IsConnected }
            };

            if (connection.IsConnected)
            {
                if (latency.TotalMilliseconds > 100)
                {
                    return HealthCheckResult.Degraded(
                        $"Redis is slow (ping: {latency.TotalMilliseconds:F2}ms)",
                        data: data);
                }

                return HealthCheckResult.Healthy(
                    $"Redis is healthy (ping: {latency.TotalMilliseconds:F2}ms)",
                    data: data);
            }

            return HealthCheckResult.Unhealthy("Redis is not connected", data: data);
        }
        catch (RedisConnectionException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Redis connection failed");

            return HealthCheckResult.Unhealthy(
                "Redis connection failed",
                ex,
                new Dictionary<string, object>
                {
                    { "ResponseTimeMs", sw.ElapsedMilliseconds },
                    { "Service", "Redis" },
                    { "Error", ex.Message }
                });
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Redis health check failed");

            return HealthCheckResult.Unhealthy(
                "Redis is unavailable",
                ex,
                new Dictionary<string, object>
                {
                    { "ResponseTimeMs", sw.ElapsedMilliseconds },
                    { "Service", "Redis" },
                    { "Error", ex.Message }
                });
        }
    }
}

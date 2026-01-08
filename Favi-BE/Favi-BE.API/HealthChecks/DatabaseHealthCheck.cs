using Favi_BE.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace Favi_BE.API.HealthChecks;

/// <summary>
/// Custom health check for PostgreSQL database connectivity
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(AppDbContext dbContext, ILogger<DatabaseHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            sw.Stop();

            var data = new Dictionary<string, object>
            {
                { "ResponseTimeMs", sw.ElapsedMilliseconds },
                { "Database", "PostgreSQL" }
            };

            if (canConnect)
            {
                // Consider connection slow if > 1 second
                if (sw.ElapsedMilliseconds > 1000)
                {
                    return HealthCheckResult.Degraded(
                        $"Database connection is slow ({sw.ElapsedMilliseconds}ms)",
                        data: data);
                }

                return HealthCheckResult.Healthy(
                    $"Database connection is healthy ({sw.ElapsedMilliseconds}ms)",
                    data: data);
            }

            return HealthCheckResult.Unhealthy("Cannot connect to database", data: data);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Database health check failed after {ElapsedMs}ms", sw.ElapsedMilliseconds);

            return HealthCheckResult.Unhealthy(
                "Database health check failed",
                ex,
                new Dictionary<string, object>
                {
                    { "ResponseTimeMs", sw.ElapsedMilliseconds },
                    { "Error", ex.Message }
                });
        }
    }
}

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Favi_BE.API.HealthChecks;

/// <summary>
/// Configuration options for memory health check
/// </summary>
public class MemoryHealthCheckOptions
{
    /// <summary>
    /// Threshold in MB above which the check is unhealthy
    /// </summary>
    public long ThresholdMB { get; set; } = 1024; // 1GB default

    /// <summary>
    /// Threshold in MB above which the check is degraded
    /// </summary>
    public long DegradedThresholdMB { get; set; } = 512; // 512MB default
}

/// <summary>
/// Health check that monitors application memory usage
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly MemoryHealthCheckOptions _options;

    public MemoryHealthCheck(IOptions<MemoryHealthCheckOptions> options)
    {
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var allocatedMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
        var workingSetMB = Environment.WorkingSet / (1024.0 * 1024.0);
        var thresholdMB = _options.ThresholdMB;
        var degradedThresholdMB = _options.DegradedThresholdMB;

        var data = new Dictionary<string, object>
        {
            { "AllocatedMB", Math.Round(allocatedMB, 2) },
            { "WorkingSetMB", Math.Round(workingSetMB, 2) },
            { "ThresholdMB", thresholdMB },
            { "DegradedThresholdMB", degradedThresholdMB },
            { "Gen0Collections", GC.CollectionCount(0) },
            { "Gen1Collections", GC.CollectionCount(1) },
            { "Gen2Collections", GC.CollectionCount(2) }
        };

        if (allocatedMB >= thresholdMB)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Memory usage is critical: {allocatedMB:F2}MB (threshold: {thresholdMB}MB)",
                data: data));
        }

        if (allocatedMB >= degradedThresholdMB)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Memory usage is high: {allocatedMB:F2}MB (degraded threshold: {degradedThresholdMB}MB)",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Memory usage is normal: {allocatedMB:F2}MB",
            data: data));
    }
}

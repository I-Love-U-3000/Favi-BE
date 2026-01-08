using Favi_BE.API.Services;
using Favi_BE.Authorization;
using Favi_BE.Data;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace Favi_BE.Controllers;

/// <summary>
/// Admin-only endpoints for system health monitoring
/// </summary>
[ApiController]
[Route("api/admin/health")]
[Authorize(Policy = AdminPolicies.RequireAdmin)]
public class AdminHealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ISystemMetricsService _metricsService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AdminHealthController> _logger;

    public AdminHealthController(
        HealthCheckService healthCheckService,
        ISystemMetricsService metricsService,
        AppDbContext dbContext,
        ILogger<AdminHealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _metricsService = metricsService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get health status of all registered health checks
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<HealthStatusResponse>> GetHealthStatus()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        var entries = report.Entries.Select(e => new HealthCheckEntryDto(
            e.Key,
            e.Value.Status.ToString(),
            e.Value.Description,
            e.Value.Duration.TotalMilliseconds,
            e.Value.Data?.ToDictionary(d => d.Key, d => d.Value),
            e.Value.Exception?.Message,
            e.Value.Tags
        ));

        return Ok(new HealthStatusResponse(
            report.Status.ToString(),
            DateTime.UtcNow,
            report.TotalDuration.TotalMilliseconds,
            entries
        ));
    }

    /// <summary>
    /// Get system metrics (CPU, Memory, GC, Process info)
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(SystemMetricsResponse), StatusCodes.Status200OK)]
    public ActionResult<SystemMetricsResponse> GetMetrics()
    {
        var metrics = _metricsService.GetCurrentMetrics();

        return Ok(new SystemMetricsResponse(
            new MemoryMetricsDto(
                metrics.WorkingSetMB,
                metrics.PrivateMemoryMB,
                metrics.GCMemoryMB
            ),
            new CpuMetricsDto(metrics.CpuUsagePercent),
            new ProcessMetricsDto(
                metrics.ThreadCount,
                metrics.HandleCount,
                metrics.UptimeSeconds,
                metrics.UptimeFormatted
            ),
            new GCMetricsDto(
                metrics.Gen0Collections,
                metrics.Gen1Collections,
                metrics.Gen2Collections
            ),
            metrics.Timestamp
        ));
    }

    /// <summary>
    /// Get detailed health including metrics and all service statuses
    /// </summary>
    [HttpGet("detailed")]
    [ProducesResponseType(typeof(DetailedHealthResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DetailedHealthResponse>> GetDetailedHealth()
    {
        var sw = Stopwatch.StartNew();

        // Get health check results
        var healthReport = await _healthCheckService.CheckHealthAsync();

        // Get system metrics
        var metrics = _metricsService.GetCurrentMetrics();

        // Extract database health from report
        var dbEntry = healthReport.Entries.FirstOrDefault(e =>
            e.Key.Equals("database", StringComparison.OrdinalIgnoreCase) ||
            e.Key.Equals("postgresql", StringComparison.OrdinalIgnoreCase));

        DatabaseHealthDto dbHealth;
        if (dbEntry.Key != null)
        {
            dbHealth = new DatabaseHealthDto(
                dbEntry.Value.Status.ToString(),
                dbEntry.Value.Description,
                dbEntry.Value.Data?.TryGetValue("ResponseTimeMs", out var responseTime) == true
                    ? Convert.ToInt64(responseTime)
                    : (long)dbEntry.Value.Duration.TotalMilliseconds
            );
        }
        else
        {
            // Fallback: check database directly
            var dbSw = Stopwatch.StartNew();
            string dbStatus;
            string? dbMessage = null;
            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync();
                dbStatus = canConnect ? "Healthy" : "Unhealthy";
            }
            catch (Exception ex)
            {
                dbStatus = "Unhealthy";
                dbMessage = ex.Message;
            }
            dbSw.Stop();

            dbHealth = new DatabaseHealthDto(dbStatus, dbMessage, dbSw.ElapsedMilliseconds);
        }

        // Extract other services (non-database checks)
        var services = healthReport.Entries
            .Where(e => !e.Key.Equals("database", StringComparison.OrdinalIgnoreCase) &&
                       !e.Key.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
            .Select(e => new ServiceHealthDto(
                e.Key,
                e.Value.Status.ToString(),
                e.Value.Description,
                e.Value.Data?.TryGetValue("ResponseTimeMs", out var rt) == true
                    ? Convert.ToInt64(rt)
                    : (long)e.Value.Duration.TotalMilliseconds,
                e.Value.Data?.ToDictionary(d => d.Key, d => d.Value)
            ));

        sw.Stop();

        return Ok(new DetailedHealthResponse(
            healthReport.Status.ToString(),
            DateTime.UtcNow,
            sw.ElapsedMilliseconds,
            new SystemMetricsResponse(
                new MemoryMetricsDto(
                    metrics.WorkingSetMB,
                    metrics.PrivateMemoryMB,
                    metrics.GCMemoryMB
                ),
                new CpuMetricsDto(metrics.CpuUsagePercent),
                new ProcessMetricsDto(
                    metrics.ThreadCount,
                    metrics.HandleCount,
                    metrics.UptimeSeconds,
                    metrics.UptimeFormatted
                ),
                new GCMetricsDto(
                    metrics.Gen0Collections,
                    metrics.Gen1Collections,
                    metrics.Gen2Collections
                ),
                metrics.Timestamp
            ),
            services,
            dbHealth
        ));
    }
}

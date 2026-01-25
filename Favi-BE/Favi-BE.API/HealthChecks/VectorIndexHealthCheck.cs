using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace Favi_BE.API.HealthChecks;

/// <summary>
/// Health check for Vector Index API service
/// </summary>
public class VectorIndexHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VectorIndexHealthCheck> _logger;

    public VectorIndexHealthCheck(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<VectorIndexHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var baseUrl = _configuration["VectorIndex:BaseUrl"] ?? "http://vector-index-api:8080";

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            // Try /healthz first (FastAPI common), fallback to /health
            var response = await client.GetAsync($"{baseUrl}/healthz", cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                response = await client.GetAsync($"{baseUrl}/health", cancellationToken);
            }
            sw.Stop();

            var data = new Dictionary<string, object>
            {
                { "ResponseTimeMs", sw.ElapsedMilliseconds },
                { "Service", "VectorIndexAPI" },
                { "Endpoint", baseUrl }
            };

            if (response.IsSuccessStatusCode)
            {
                if (sw.ElapsedMilliseconds > 2000)
                {
                    return HealthCheckResult.Degraded(
                        $"Vector Index API is slow ({sw.ElapsedMilliseconds}ms)",
                        data: data);
                }

                return HealthCheckResult.Healthy(
                    $"Vector Index API is healthy ({sw.ElapsedMilliseconds}ms)",
                    data: data);
            }

            data["StatusCode"] = (int)response.StatusCode;
            return HealthCheckResult.Unhealthy(
                $"Vector Index API returned {response.StatusCode}",
                data: data);
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            return HealthCheckResult.Unhealthy(
                "Vector Index API request timed out",
                data: new Dictionary<string, object>
                {
                    { "ResponseTimeMs", sw.ElapsedMilliseconds },
                    { "Service", "VectorIndexAPI" },
                    { "Error", "Timeout" }
                });
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Vector Index API health check failed");

            return HealthCheckResult.Unhealthy(
                "Vector Index API is unavailable",
                ex,
                new Dictionary<string, object>
                {
                    { "ResponseTimeMs", sw.ElapsedMilliseconds },
                    { "Service", "VectorIndexAPI" },
                    { "Error", ex.Message }
                });
        }
    }
}

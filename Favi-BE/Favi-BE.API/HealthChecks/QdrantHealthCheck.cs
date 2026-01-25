using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace Favi_BE.API.HealthChecks;

/// <summary>
/// Health check for Qdrant vector database service
/// </summary>
public class QdrantHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QdrantHealthCheck> _logger;

    public QdrantHealthCheck(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<QdrantHealthCheck> logger)
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
        var baseUrl = _configuration["Qdrant:BaseUrl"] ?? "http://qdrant:6333";

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            // Qdrant health endpoint
            var response = await client.GetAsync($"{baseUrl}/healthz", cancellationToken);
            sw.Stop();

            var data = new Dictionary<string, object>
            {
                { "ResponseTimeMs", sw.ElapsedMilliseconds },
                { "Service", "Qdrant" },
                { "Endpoint", baseUrl }
            };

            if (response.IsSuccessStatusCode)
            {
                if (sw.ElapsedMilliseconds > 2000)
                {
                    return HealthCheckResult.Degraded(
                        $"Qdrant is slow ({sw.ElapsedMilliseconds}ms)",
                        data: data);
                }

                return HealthCheckResult.Healthy(
                    $"Qdrant is healthy ({sw.ElapsedMilliseconds}ms)",
                    data: data);
            }

            data["StatusCode"] = (int)response.StatusCode;
            return HealthCheckResult.Unhealthy(
                $"Qdrant returned {response.StatusCode}",
                data: data);
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            return HealthCheckResult.Unhealthy(
                "Qdrant request timed out",
                data: new Dictionary<string, object>
                {
                    { "ResponseTimeMs", sw.ElapsedMilliseconds },
                    { "Service", "Qdrant" },
                    { "Error", "Timeout" }
                });
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Qdrant health check failed");

            return HealthCheckResult.Unhealthy(
                "Qdrant is unavailable",
                ex,
                new Dictionary<string, object>
                {
                    { "ResponseTimeMs", sw.ElapsedMilliseconds },
                    { "Service", "Qdrant" },
                    { "Error", ex.Message }
                });
        }
    }
}

namespace Favi_BE.Models.Dtos;

/// <summary>
/// Response for overall health status with all checks
/// </summary>
public record HealthStatusResponse(
    string Status,
    DateTime Timestamp,
    double TotalDurationMs,
    IEnumerable<HealthCheckEntryDto> Entries
);

/// <summary>
/// Individual health check entry
/// </summary>
public record HealthCheckEntryDto(
    string Name,
    string Status,
    string? Description,
    double DurationMs,
    IReadOnlyDictionary<string, object>? Data,
    string? Exception,
    IEnumerable<string> Tags
);

/// <summary>
/// Complete system metrics response
/// </summary>
public record SystemMetricsResponse(
    MemoryMetricsDto Memory,
    CpuMetricsDto Cpu,
    ProcessMetricsDto Process,
    GCMetricsDto GarbageCollection,
    DateTime Timestamp
);

/// <summary>
/// Memory-related metrics
/// </summary>
public record MemoryMetricsDto(
    double WorkingSetMB,
    double PrivateMemoryMB,
    double GCMemoryMB
);

/// <summary>
/// CPU-related metrics
/// </summary>
public record CpuMetricsDto(
    double UsagePercent
);

/// <summary>
/// Process-related metrics
/// </summary>
public record ProcessMetricsDto(
    int ThreadCount,
    int HandleCount,
    double UptimeSeconds,
    string UptimeFormatted
);

/// <summary>
/// Garbage collection metrics
/// </summary>
public record GCMetricsDto(
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections
);

/// <summary>
/// Detailed health response including metrics and service statuses
/// </summary>
public record DetailedHealthResponse(
    string OverallStatus,
    DateTime Timestamp,
    double TotalCheckDurationMs,
    SystemMetricsResponse Metrics,
    IEnumerable<ServiceHealthDto> Services,
    DatabaseHealthDto Database
);

/// <summary>
/// Individual service health status
/// </summary>
public record ServiceHealthDto(
    string Name,
    string Status,
    string? Message,
    long ResponseTimeMs,
    IReadOnlyDictionary<string, object>? Data
);

/// <summary>
/// Database health status
/// </summary>
public record DatabaseHealthDto(
    string Status,
    string? Message,
    long ResponseTimeMs
);

/// <summary>
/// Simple health response for basic endpoints
/// </summary>
public record SimpleHealthResponse(
    string Status,
    DateTime Timestamp
);

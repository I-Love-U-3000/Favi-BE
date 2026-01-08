using System.Diagnostics;

namespace Favi_BE.API.Services;

/// <summary>
/// Interface for system metrics collection
/// </summary>
public interface ISystemMetricsService
{
    SystemMetrics GetCurrentMetrics();
}

/// <summary>
/// Service to collect system metrics like CPU, Memory, GC statistics
/// </summary>
public class SystemMetricsService : ISystemMetricsService
{
    private readonly Process _currentProcess;
    private DateTime _lastCpuTime;
    private TimeSpan _lastTotalProcessorTime;

    public SystemMetricsService()
    {
        _currentProcess = Process.GetCurrentProcess();
        _lastCpuTime = DateTime.UtcNow;
        _lastTotalProcessorTime = _currentProcess.TotalProcessorTime;
    }

    public SystemMetrics GetCurrentMetrics()
    {
        _currentProcess.Refresh();

        // Memory metrics
        var workingSetMB = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
        var privateMemoryMB = _currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0);
        var gcMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

        // CPU usage calculation
        var currentCpuTime = DateTime.UtcNow;
        var currentTotalProcessorTime = _currentProcess.TotalProcessorTime;

        var cpuUsedMs = (currentTotalProcessorTime - _lastTotalProcessorTime).TotalMilliseconds;
        var totalMsPassed = (currentCpuTime - _lastCpuTime).TotalMilliseconds;
        var cpuUsagePercent = totalMsPassed > 0
            ? (cpuUsedMs / (Environment.ProcessorCount * totalMsPassed)) * 100
            : 0;

        _lastCpuTime = currentCpuTime;
        _lastTotalProcessorTime = currentTotalProcessorTime;

        // Uptime
        var uptime = DateTime.UtcNow - _currentProcess.StartTime.ToUniversalTime();

        return new SystemMetrics
        {
            WorkingSetMB = Math.Round(workingSetMB, 2),
            PrivateMemoryMB = Math.Round(privateMemoryMB, 2),
            GCMemoryMB = Math.Round(gcMemoryMB, 2),
            CpuUsagePercent = Math.Round(Math.Min(cpuUsagePercent, 100), 2),
            ThreadCount = _currentProcess.Threads.Count,
            HandleCount = _currentProcess.HandleCount,
            UptimeSeconds = Math.Round(uptime.TotalSeconds, 0),
            UptimeFormatted = FormatUptime(uptime),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            Timestamp = DateTime.UtcNow
        };
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        if (uptime.TotalHours >= 1)
            return $"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s";
        if (uptime.TotalMinutes >= 1)
            return $"{(int)uptime.TotalMinutes}m {uptime.Seconds}s";
        return $"{(int)uptime.TotalSeconds}s";
    }
}

/// <summary>
/// System metrics data model
/// </summary>
public class SystemMetrics
{
    public double WorkingSetMB { get; set; }
    public double PrivateMemoryMB { get; set; }
    public double GCMemoryMB { get; set; }
    public double CpuUsagePercent { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public double UptimeSeconds { get; set; }
    public string UptimeFormatted { get; set; } = string.Empty;
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    public DateTime Timestamp { get; set; }
}

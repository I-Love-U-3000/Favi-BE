using Favi_BE.Authorization;
using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers;

/// <summary>
/// Admin endpoints for exporting data
/// </summary>
[ApiController]
[Route("api/admin/export")]
[Authorize(Policy = AdminPolicies.RequireAdmin)]
public class AdminExportController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly IAuditService _auditService;

    public AdminExportController(IExportService exportService, IAuditService auditService)
    {
        _exportService = exportService;
        _auditService = auditService;
    }

    // ============================================================
    // EXPORT USERS
    // ============================================================

    /// <summary>
    /// Export users data to file
    /// </summary>
    /// <param name="search">Search by username or display name</param>
    /// <param name="role">Filter by role (User, Admin)</param>
    /// <param name="status">Filter by status (active, banned)</param>
    /// <param name="fromDate">Filter from date</param>
    /// <param name="toDate">Filter to date</param>
    /// <param name="format">Export format: csv, json, excel (default: csv)</param>
    [HttpGet("users")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string format = "csv")
    {
        var request = new ExportUsersRequest(search, role, status, fromDate, toDate, ParseFormat(format));
        var data = await _exportService.GetUsersForExportAsync(request);

        var headers = new[]
        {
            "ID", "Username", "DisplayName", "Email", "Role", "IsBanned", "BannedUntil",
            "CreatedAt", "LastActiveAt", "PostsCount", "FollowersCount", "FollowingCount"
        };

        // Log export action
        var adminId = User.GetUserIdFromMetadata();
        await _auditService.LogExportAsync(adminId, "Users", data.Count(), format);

        return GenerateFileResult(data, headers, "users", request.Format);
    }

    // ============================================================
    // EXPORT POSTS
    // ============================================================

    /// <summary>
    /// Export posts data to file
    /// </summary>
    [HttpGet("posts")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportPosts(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string format = "csv")
    {
        var request = new ExportPostsRequest(search, status, fromDate, toDate, ParseFormat(format));
        var data = await _exportService.GetPostsForExportAsync(request);

        var headers = new[]
        {
            "ID", "AuthorID", "AuthorUsername", "Caption", "Privacy",
            "CreatedAt", "IsDeleted", "ReactionsCount", "CommentsCount", "MediaCount"
        };

        var adminId = User.GetUserIdFromMetadata();
        await _auditService.LogExportAsync(adminId, "Posts", data.Count(), format);

        return GenerateFileResult(data, headers, "posts", request.Format);
    }

    // ============================================================
    // EXPORT REPORTS
    // ============================================================

    /// <summary>
    /// Export reports data to file
    /// </summary>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportReports(
        [FromQuery] string? status,
        [FromQuery] string? targetType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string format = "csv")
    {
        var request = new ExportReportsRequest(status, targetType, fromDate, toDate, ParseFormat(format));
        var data = await _exportService.GetReportsForExportAsync(request);

        var headers = new[]
        {
            "ID", "ReporterID", "ReporterUsername", "TargetType", "TargetID",
            "Reason", "Status", "CreatedAt", "ActedAt"
        };

        var adminId = User.GetUserIdFromMetadata();
        await _auditService.LogExportAsync(adminId, "Reports", data.Count(), format);

        return GenerateFileResult(data, headers, "reports", request.Format);
    }

    // ============================================================
    // EXPORT AUDIT LOGS
    // ============================================================

    /// <summary>
    /// Export audit logs to file
    /// </summary>
    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] string? actionType,
        [FromQuery] Guid? adminId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string format = "csv")
    {
        var request = new ExportAuditLogsRequest(actionType, adminId, fromDate, toDate, ParseFormat(format));
        var data = await _exportService.GetAuditLogsForExportAsync(request);

        var headers = new[]
        {
            "ID", "AdminID", "AdminUsername", "ActionType", "TargetProfileID",
            "TargetUsername", "TargetEntityType", "TargetEntityID", "Notes", "CreatedAt"
        };

        var currentAdminId = User.GetUserIdFromMetadata();
        await _auditService.LogExportAsync(currentAdminId, "AuditLogs", data.Count(), format);

        return GenerateFileResult(data, headers, "audit-logs", request.Format);
    }

    // ============================================================
    // EXPORT ANALYTICS/CHARTS DATA
    // ============================================================

    /// <summary>
    /// Export growth chart data
    /// </summary>
    [HttpGet("charts/growth")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportGrowthChart(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string interval = "day",
        [FromQuery] string format = "csv",
        [FromServices] IAnalyticsService analyticsService = null!)
    {
        var data = await analyticsService.GetGrowthChartAsync(fromDate, toDate, interval);

        // Flatten data for export
        var exportData = data.Users.Select((u, i) => new
        {
            Date = u.Date.ToString("yyyy-MM-dd"),
            Users = u.Count,
            Posts = data.Posts.ElementAtOrDefault(i)?.Count ?? 0,
            Reports = data.Reports.ElementAtOrDefault(i)?.Count ?? 0
        });

        var headers = new[] { "Date", "Users", "Posts", "Reports" };

        var adminId = User.GetUserIdFromMetadata();
        await _auditService.LogExportAsync(adminId, "GrowthChart", exportData.Count(), format);

        return GenerateFileResult(exportData, headers, "growth-chart", ParseFormat(format));
    }

    /// <summary>
    /// Export dashboard summary
    /// </summary>
    [HttpGet("dashboard-summary")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportDashboardSummary(
        [FromQuery] string format = "json",
        [FromServices] IAnalyticsService analyticsService = null!)
    {
        var stats = await analyticsService.GetDashboardStatsAsync();
        var userStatus = await analyticsService.GetUserStatusDistributionAsync();
        var reportStatus = await analyticsService.GetReportStatusDistributionAsync();

        var summary = new
        {
            GeneratedAt = DateTime.UtcNow,
            Stats = stats,
            UserDistribution = userStatus,
            ReportDistribution = reportStatus
        };

        var adminId = User.GetUserIdFromMetadata();
        await _auditService.LogExportAsync(adminId, "DashboardSummary", 1, format);

        var exportFormat = ParseFormat(format);
        var bytes = _exportService.GenerateJson(new[] { summary });
        var filename = $"dashboard-summary-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";

        return File(bytes, "application/json", filename);
    }

    // ============================================================
    // HELPER METHODS
    // ============================================================

    private static ExportFormat ParseFormat(string format)
    {
        return format.ToLower() switch
        {
            "json" => ExportFormat.Json,
            "excel" or "xlsx" => ExportFormat.Excel,
            _ => ExportFormat.Csv
        };
    }

    private IActionResult GenerateFileResult<T>(IEnumerable<T> data, string[] headers, string baseFilename, ExportFormat format)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

        return format switch
        {
            ExportFormat.Json => File(
                _exportService.GenerateJson(data),
                "application/json",
                $"{baseFilename}-{timestamp}.json"),

            ExportFormat.Excel => File(
                _exportService.GenerateExcel(data, baseFilename, headers),
                "application/vnd.ms-excel",
                $"{baseFilename}-{timestamp}.xml"),

            _ => File(
                _exportService.GenerateCsv(data, headers),
                "text/csv; charset=utf-8",
                $"{baseFilename}-{timestamp}.csv")
        };
    }
}

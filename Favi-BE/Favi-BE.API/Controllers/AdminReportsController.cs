using Favi_BE.Authorization;
using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers;

/// <summary>
/// Admin endpoints for report management
/// </summary>
[ApiController]
[Route("api/admin/reports")]
[Authorize(Policy = AdminPolicies.RequireAdmin)]
public class AdminReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IBulkActionService _bulkService;

    public AdminReportsController(
        IReportService reportService,
        IBulkActionService bulkService)
    {
        _reportService = reportService;
        _bulkService = bulkService;
    }

    // ============================================================
    // QUERY REPORTS
    // ============================================================

    /// <summary>
    /// Get all reports with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReportResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReportResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _reportService.GetAllAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get reports filtered by status
    /// </summary>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(PagedResult<ReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ReportResponse>>> GetByStatus(
        string status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!Enum.TryParse<ReportStatus>(status, true, out var reportStatus))
            return BadRequest(new { code = "INVALID_STATUS", message = $"Invalid status: {status}" });

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        // Get all and filter by status (could be optimized with a new repository method)
        var allReports = await _reportService.GetAllAsync(1, int.MaxValue);
        var filtered = allReports.Items
            .Where(r => r.Status == reportStatus)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var total = allReports.Items.Count(r => r.Status == reportStatus);
        return Ok(new PagedResult<ReportResponse>(filtered, page, pageSize, total));
    }

    /// <summary>
    /// Get reports by target type (Post, Comment, User)
    /// </summary>
    [HttpGet("target-type/{targetType}")]
    [ProducesResponseType(typeof(PagedResult<ReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ReportResponse>>> GetByTargetType(
        string targetType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!Enum.TryParse<ReportTarget>(targetType, true, out var reportTarget))
            return BadRequest(new { code = "INVALID_TARGET_TYPE", message = $"Invalid target type: {targetType}" });

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _reportService.GetReportsByTargetTypeAsync(reportTarget, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get reports for a specific target
    /// </summary>
    [HttpGet("target/{targetId:guid}")]
    [ProducesResponseType(typeof(PagedResult<ReportResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReportResponse>>> GetByTargetId(
        Guid targetId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _reportService.GetReportsByTargetIdAsync(targetId, page, pageSize);
        return Ok(result);
    }

    // ============================================================
    // SINGLE REPORT ACTIONS
    // ============================================================

    /// <summary>
    /// Update status of a single report
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateReportStatusRequest request)
    {
        var adminId = User.GetUserIdFromMetadata();
        var ok = await _reportService.UpdateStatusAsync(id, request, adminId);

        return ok
            ? Ok(new { message = "Report status updated successfully." })
            : NotFound(new { code = "REPORT_NOT_FOUND", message = "Report not found." });
    }

    /// <summary>
    /// Resolve a single report
    /// </summary>
    [HttpPost("{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolve(Guid id)
    {
        var adminId = User.GetUserIdFromMetadata();
        var request = new UpdateReportStatusRequest(ReportStatus.Resolved);
        var ok = await _reportService.UpdateStatusAsync(id, request, adminId);

        return ok
            ? Ok(new { message = "Report resolved successfully." })
            : NotFound(new { code = "REPORT_NOT_FOUND", message = "Report not found." });
    }

    /// <summary>
    /// Reject a single report
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(Guid id)
    {
        var adminId = User.GetUserIdFromMetadata();
        var request = new UpdateReportStatusRequest(ReportStatus.Rejected);
        var ok = await _reportService.UpdateStatusAsync(id, request, adminId);

        return ok
            ? Ok(new { message = "Report rejected successfully." })
            : NotFound(new { code = "REPORT_NOT_FOUND", message = "Report not found." });
    }

    // ============================================================
    // BULK REPORT ACTIONS
    // ============================================================

    /// <summary>
    /// Resolve multiple reports at once (max 100)
    /// </summary>
    [HttpPost("bulk/resolve")]
    [ProducesResponseType(typeof(BulkActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkActionResponse>> BulkResolve([FromBody] BulkResolveReportsRequest request)
    {
        if (request.ReportIds == null || !request.ReportIds.Any())
            return BadRequest(new { code = "NO_REPORT_IDS", message = "At least one report ID is required." });

        var adminId = User.GetUserIdFromMetadata();
        var result = await _bulkService.BulkResolveReportsAsync(
            request.ReportIds,
            adminId,
            ReportStatus.Resolved);

        return Ok(result);
    }

    /// <summary>
    /// Reject multiple reports at once (max 100)
    /// </summary>
    [HttpPost("bulk/reject")]
    [ProducesResponseType(typeof(BulkActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkActionResponse>> BulkReject([FromBody] BulkRejectReportsRequest request)
    {
        if (request.ReportIds == null || !request.ReportIds.Any())
            return BadRequest(new { code = "NO_REPORT_IDS", message = "At least one report ID is required." });

        var adminId = User.GetUserIdFromMetadata();
        var result = await _bulkService.BulkResolveReportsAsync(
            request.ReportIds,
            adminId,
            ReportStatus.Rejected);

        return Ok(result);
    }
}

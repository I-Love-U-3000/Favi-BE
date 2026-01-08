using Favi_BE.Authorization;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers;

/// <summary>
/// Admin endpoints for viewing audit logs
/// </summary>
[ApiController]
[Route("api/admin/audit")]
[Authorize(Policy = AdminPolicies.RequireAdmin)]
public class AdminAuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AdminAuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Get paginated audit logs with filters
    /// </summary>
    /// <param name="actionType">Filter by action type (e.g., BanUser, DeleteContent)</param>
    /// <param name="adminId">Filter by admin who performed the action</param>
    /// <param name="targetProfileId">Filter by target profile</param>
    /// <param name="fromDate">Filter from date (inclusive)</param>
    /// <param name="toDate">Filter to date (inclusive)</param>
    /// <param name="search">Search in notes</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetLogs(
        [FromQuery] AdminActionType? actionType,
        [FromQuery] Guid? adminId,
        [FromQuery] Guid? targetProfileId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Validate and clamp parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var filter = new AuditLogFilterRequest
        {
            ActionType = actionType,
            AdminId = adminId,
            TargetProfileId = targetProfileId,
            FromDate = fromDate,
            ToDate = toDate,
            Search = search,
            Page = page,
            PageSize = pageSize
        };

        var result = await _auditService.GetLogsAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific audit log by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AuditLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditLogDto>> GetLogById(Guid id)
    {
        var log = await _auditService.GetLogByIdAsync(id);
        if (log == null)
            return NotFound(new { code = "AUDIT_LOG_NOT_FOUND", message = "Audit log not found." });

        return Ok(log);
    }

    /// <summary>
    /// Get all available action types
    /// </summary>
    [HttpGet("action-types")]
    [ProducesResponseType(typeof(IEnumerable<ActionTypeInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ActionTypeInfo>>> GetActionTypes()
    {
        var types = await _auditService.GetActionTypesAsync();
        return Ok(types);
    }

    /// <summary>
    /// Get action type summary with counts
    /// </summary>
    /// <param name="fromDate">Filter from date (optional)</param>
    /// <param name="toDate">Filter to date (optional)</param>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(IEnumerable<AuditActionTypeSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AuditActionTypeSummary>>> GetSummary(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var summary = await _auditService.GetActionTypeSummaryAsync(fromDate, toDate);
        return Ok(summary);
    }
}

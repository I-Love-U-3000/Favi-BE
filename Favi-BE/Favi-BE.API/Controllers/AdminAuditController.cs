using Favi_BE.Authorization;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.Moderation.Application.Queries.GetAdminActionAudit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModDomain = Favi_BE.Modules.Moderation.Domain;

namespace Favi_BE.Controllers;

[ApiController]
[Route("api/admin/audit")]
[Authorize(Policy = AdminPolicies.RequireAdmin)]
public class AdminAuditController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAuditService _auditService;

    public AdminAuditController(IMediator mediator, IAuditService auditService)
    {
        _mediator = mediator;
        _auditService = auditService;
    }

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
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var modActionType = actionType.HasValue
            ? (ModDomain.AdminActionType?)(int)actionType.Value
            : null;

        var (items, total) = await _mediator.Send(new GetAdminActionAuditQuery(
            page, pageSize, modActionType, adminId, targetProfileId, fromDate, toDate, search));

        var dtos = items.Select(a => new AuditLogDto(
            a.Id, a.AdminId,
            a.AdminUsername, a.AdminDisplayName,
            (AdminActionType)(int)a.ActionType,
            a.ActionType.ToString(),
            a.TargetProfileId, a.TargetUsername, a.TargetDisplayName,
            a.TargetEntityId, a.TargetEntityType,
            a.ReportId, a.Notes, a.CreatedAt)).ToList();

        return Ok(new PagedResult<AuditLogDto>(dtos, page, pageSize, total));
    }

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

    [HttpGet("action-types")]
    [ProducesResponseType(typeof(IEnumerable<ActionTypeInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ActionTypeInfo>>> GetActionTypes()
    {
        var types = await _auditService.GetActionTypesAsync();
        return Ok(types);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(IEnumerable<AuditActionTypeSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AuditActionTypeSummary>>> GetSummary(
        [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var summary = await _auditService.GetActionTypeSummaryAsync(fromDate, toDate);
        return Ok(summary);
    }
}

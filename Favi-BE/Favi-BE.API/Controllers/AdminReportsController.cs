using Favi_BE.Authorization;
using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.Moderation.Application.Commands.ResolveReport;
using Favi_BE.Modules.Moderation.Application.Queries.GetReportById;
using Favi_BE.Modules.Moderation.Application.Queries.GetReports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModDomain = Favi_BE.Modules.Moderation.Domain;

namespace Favi_BE.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Policy = AdminPolicies.RequireAdmin)]
public class AdminReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IBulkActionService _bulkService;

    public AdminReportsController(IMediator mediator, IBulkActionService bulkService)
    {
        _mediator = mediator;
        _bulkService = bulkService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReportResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReportResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var (items, total) = await _mediator.Send(new GetReportsQuery(page, pageSize, null, null, null));
        return Ok(new PagedResult<ReportResponse>(items.Select(MapReport).ToList(), page, pageSize, total));
    }

    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(PagedResult<ReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ReportResponse>>> GetByStatus(
        string status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!Enum.TryParse<ReportStatus>(status, true, out var reportStatus))
            return BadRequest(new { code = "INVALID_STATUS", message = $"Invalid status: {status}" });

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var (items, total) = await _mediator.Send(new GetReportsQuery(
            page, pageSize, (ModDomain.ReportStatus)(int)reportStatus, null, null));
        return Ok(new PagedResult<ReportResponse>(items.Select(MapReport).ToList(), page, pageSize, total));
    }

    [HttpGet("target-type/{targetType}")]
    [ProducesResponseType(typeof(PagedResult<ReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ReportResponse>>> GetByTargetType(
        string targetType, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!Enum.TryParse<ReportTarget>(targetType, true, out var reportTarget))
            return BadRequest(new { code = "INVALID_TARGET_TYPE", message = $"Invalid target type: {targetType}" });

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var (items, total) = await _mediator.Send(new GetReportsQuery(
            page, pageSize, null, (ModDomain.ReportTarget)(int)reportTarget, null));
        return Ok(new PagedResult<ReportResponse>(items.Select(MapReport).ToList(), page, pageSize, total));
    }

    [HttpGet("target/{targetId:guid}")]
    [ProducesResponseType(typeof(PagedResult<ReportResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReportResponse>>> GetByTargetId(
        Guid targetId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var (items, total) = await _mediator.Send(new GetReportsQuery(page, pageSize, null, null, null));
        var filtered = items.Where(r => r.TargetId == targetId).ToList();
        return Ok(new PagedResult<ReportResponse>(filtered.Select(MapReport).ToList(), page, pageSize, filtered.Count));
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateReportStatusRequest request)
    {
        var adminId = User.GetUserIdFromMetadata();
        var result = await _mediator.Send(new ResolveReportCommand(
            id, adminId, (ModDomain.ReportStatus)(int)request.NewStatus, null));

        return result.Succeeded
            ? Ok(new { message = "Report status updated successfully." })
            : NotFound(new { code = "REPORT_NOT_FOUND", message = "Report not found." });
    }

    [HttpPost("{id:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolve(Guid id)
    {
        var adminId = User.GetUserIdFromMetadata();
        var result = await _mediator.Send(new ResolveReportCommand(
            id, adminId, ModDomain.ReportStatus.Resolved, null));

        return result.Succeeded
            ? Ok(new { message = "Report resolved successfully." })
            : NotFound(new { code = "REPORT_NOT_FOUND", message = "Report not found." });
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(Guid id)
    {
        var adminId = User.GetUserIdFromMetadata();
        var result = await _mediator.Send(new ResolveReportCommand(
            id, adminId, ModDomain.ReportStatus.Rejected, null));

        return result.Succeeded
            ? Ok(new { message = "Report rejected successfully." })
            : NotFound(new { code = "REPORT_NOT_FOUND", message = "Report not found." });
    }

    [HttpPost("bulk/resolve")]
    [ProducesResponseType(typeof(BulkActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkActionResponse>> BulkResolve([FromBody] BulkResolveReportsRequest request)
    {
        if (request.ReportIds == null || !request.ReportIds.Any())
            return BadRequest(new { code = "NO_REPORT_IDS", message = "At least one report ID is required." });

        var adminId = User.GetUserIdFromMetadata();
        var result = await _bulkService.BulkResolveReportsAsync(request.ReportIds, adminId, ReportStatus.Resolved);
        return Ok(result);
    }

    [HttpPost("bulk/reject")]
    [ProducesResponseType(typeof(BulkActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkActionResponse>> BulkReject([FromBody] BulkRejectReportsRequest request)
    {
        if (request.ReportIds == null || !request.ReportIds.Any())
            return BadRequest(new { code = "NO_REPORT_IDS", message = "At least one report ID is required." });

        var adminId = User.GetUserIdFromMetadata();
        var result = await _bulkService.BulkResolveReportsAsync(request.ReportIds, adminId, ReportStatus.Rejected);
        return Ok(result);
    }

    private static ReportResponse MapReport(Favi_BE.Modules.Moderation.Application.Contracts.ReadModels.ReportReadModel r) =>
        new(r.Id, r.ReporterId, (ReportTarget)(int)r.TargetType, r.TargetId,
            r.Reason ?? string.Empty, (ReportStatus)(int)r.Status,
            r.CreatedAt, r.ActedAt, r.Data);
}

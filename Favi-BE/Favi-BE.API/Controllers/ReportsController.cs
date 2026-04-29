using Favi_BE.Authorization;
using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.Moderation.Application.Commands.CreateReport;
using Favi_BE.Modules.Moderation.Application.Queries.GetReportById;
using Favi_BE.Modules.Moderation.Application.Queries.GetReports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModDomain = Favi_BE.Modules.Moderation.Domain;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IPrivacyGuard _privacy;

        public ReportsController(IMediator mediator, IPrivacyGuard privacy)
        {
            _mediator = mediator;
            _privacy = privacy;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ReportResponse>> Create(CreateReportRequest dto)
        {
            var reporterId = User.GetUserId();
            if (!await _privacy.CanReportAsync(dto.TargetType, dto.TargetId, reporterId))
                return StatusCode(403, new { code = "REPORT_FORBIDDEN", message = "Bạn không thể báo cáo nội dung này." });

            var result = await _mediator.Send(new CreateReportCommand(
                reporterId,
                (ModDomain.ReportTarget)(int)dto.TargetType,
                dto.TargetId,
                dto.Reason));

            if (result is null)
                return NotFound(new { code = "REPORTER_NOT_FOUND" });

            return Ok(MapReport(result));
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<ReportResponse>>> GetAll(int page = 1, int pageSize = 20)
        {
            var (items, total) = await _mediator.Send(new GetReportsQuery(page, pageSize, null, null, null));
            return Ok(new PagedResult<ReportResponse>(items.Select(MapReport).ToList(), page, pageSize, total));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(Guid id, UpdateReportStatusRequest dto)
        {
            var adminId = User.GetUserId();
            var resolution = (ModDomain.ReportStatus)(int)dto.NewStatus;
            var result = await _mediator.Send(new Favi_BE.Modules.Moderation.Application.Commands.ResolveReport.ResolveReportCommand(
                id, adminId, resolution, null));

            return result.Succeeded
                ? Ok(new { message = "Đã cập nhật trạng thái báo cáo." })
                : NotFound(new { code = "REPORT_NOT_FOUND", message = "Không tìm thấy báo cáo." });
        }

        [Authorize]
        [HttpGet("my")]
        public async Task<ActionResult<PagedResult<ReportResponse>>> GetMyReports(int page = 1, int pageSize = 20)
        {
            var userId = User.GetUserId();
            var (items, total) = await _mediator.Send(new GetReportsQuery(page, pageSize, null, null, userId));
            return Ok(new PagedResult<ReportResponse>(items.Select(MapReport).ToList(), page, pageSize, total));
        }

        [Authorize]
        [HttpGet("target/{targetId}")]
        public async Task<ActionResult<PagedResult<ReportResponse>>> GetReportsByTarget(Guid targetId, int page = 1, int pageSize = 20)
        {
            var userId = User.GetUserId();
            if (userId != targetId && !User.IsInRole("admin"))
                return StatusCode(403, new { code = "REPORT_LIST_FORBIDDEN", message = "Bạn không có quyền xem báo cáo cho mục tiêu này." });

            var (items, total) = await _mediator.Send(new GetReportsQuery(page, pageSize, null, null, null));
            var filtered = items.Where(r => r.TargetId == targetId).ToList();
            return Ok(new PagedResult<ReportResponse>(filtered.Select(MapReport).ToList(), page, pageSize, filtered.Count));
        }

        [Authorize]
        [HttpGet("target-type")]
        public async Task<ActionResult<PagedResult<ReportResponse>>> GetReportsByTargetType([FromQuery] string targetType, int page = 1, int pageSize = 20)
        {
            if (!User.IsInRole("admin"))
                return StatusCode(403, new { code = "REPORT_LIST_FORBIDDEN", message = "Chỉ admin mới được xem danh sách báo cáo theo loại." });

            if (!Enum.TryParse<ReportTarget>(targetType, true, out var reportTarget))
                return BadRequest(new { code = "INVALID_REPORT_TARGET", message = $"Giá trị targetType '{targetType}' không hợp lệ." });

            var (items, total) = await _mediator.Send(new GetReportsQuery(page, pageSize, null, (ModDomain.ReportTarget)(int)reportTarget, null));
            return Ok(new PagedResult<ReportResponse>(items.Select(MapReport).ToList(), page, pageSize, total));
        }

        private static ReportResponse MapReport(Favi_BE.Modules.Moderation.Application.Contracts.ReadModels.ReportReadModel r) =>
            new(r.Id, r.ReporterId, (ReportTarget)(int)r.TargetType, r.TargetId,
                r.Reason ?? string.Empty, (ReportStatus)(int)r.Status,
                r.CreatedAt, r.ActedAt, r.Data);
    }
}

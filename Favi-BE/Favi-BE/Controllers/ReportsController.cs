﻿using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reports;
        private readonly IPrivacyGuard _privacy;
        public ReportsController(
            IReportService reports ,
            IPrivacyGuard privacy
            )
        {
            _reports = reports;
            _privacy = privacy;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ReportResponse>> Create(CreateReportRequest dto)
        {
            var reporterId = User.GetUserIdFromMetadata();
            if (!await _privacy.CanReportAsync(dto.TargetType, dto.TargetId, reporterId))
                return StatusCode(403, new { code = "REPORT_FORBIDDEN", message = "Bạn không thể báo cáo nội dung này." });

            var report = await _reports.CreateAsync(dto);
            return Ok(report);
        }

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<ActionResult<PagedResult<ReportResponse>>> GetAll(int page = 1, int pageSize = 20) =>
            Ok(await _reports.GetAllAsync(page, pageSize));

        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(Guid id, UpdateReportStatusRequest dto)
        {
            var ok = await _reports.UpdateStatusAsync(id, dto);
            return ok
                ? Ok(new { message = "Đã cập nhật trạng thái báo cáo." })
                : NotFound(new { code = "REPORT_NOT_FOUND", message = "Không tìm thấy báo cáo." });
        }

        [Authorize]
        [HttpGet("my")]
        public async Task<ActionResult<PagedResult<ReportResponse>>> GetMyReports(int page = 1, int pageSize = 20)
        {
            var userId = User.GetUserIdFromMetadata();
            return Ok(await _reports.GetReportsByReporterIdAsync(userId, page, pageSize));
        }

        // dung policy thay vi check role
        [Authorize]
        [HttpGet("target/{targetId}")]
        public async Task<ActionResult<PagedResult<ReportResponse>>> GetReportsByTarget(Guid targetId, int page = 1, int pageSize = 20)
        {
            var userId = User.GetUserIdFromMetadata();
            if (userId != targetId && !User.IsInRole("admin"))
            {
                return Forbid();
            }
            return Ok(await _reports.GetReportsByTargetIdAsync(targetId, page, pageSize));
        }

        [Authorize]
        [HttpGet("target-type")]
        public async Task<ActionResult<PagedResult<ReportResponse>>> GetReportsByTargetType([FromQuery] string targetType, int page = 1, int pageSize = 20)
        {
            if (!User.IsInRole("admin"))
            {
                return Forbid();
            }
            var parsed = Enum.TryParse<ReportTarget>(targetType, true, out var reportTarget);
            return Ok(await _reports.GetReportsByTargetTypeAsync(reportTarget, page, pageSize));
        }
    }
}
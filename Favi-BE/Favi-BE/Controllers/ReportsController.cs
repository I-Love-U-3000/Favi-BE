using Favi_BE.Common;
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
        public ReportsController(IReportService reports) => _reports = reports;

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<ReportResponse>> Create(CreateReportRequest dto) =>
            Ok(await _reports.CreateAsync(dto));

        [Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<ActionResult<PagedResult<ReportResponse>>> GetAll(int page = 1, int pageSize = 20) =>
            Ok(await _reports.GetAllAsync(page, pageSize));

        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(Guid id, UpdateReportStatusRequest dto)
        {
            var ok = await _reports.UpdateStatusAsync(id, dto);
            return ok ? Ok() : NotFound();
        }

        [Authorize]
        [HttpGet("my")]
        public async Task<ActionResult<PagedResult<ReportResponse>>> GetMyReports(int page = 1, int pageSize = 20)
        {
            var userId = User.GetUserIdFromMetadata();
            return Ok(await _reports.GetReportsByReporterIdAsync(userId, page, pageSize));
        }

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
        [HttpGet("target-type/{targetType}")]
        public async Task<ActionResult<PagedResult<ReportResponse>>> GetReportsByTargetType(string targetType, int page = 1, int pageSize = 20)
        {
            if (!User.IsInRole("admin"))
            {
                return Forbid();
            }
            ReportTarget reportTarget; 
            switch (targetType.ToLower())
            {
                case "post":
                    reportTarget = ReportTarget.Post;
                    break;
                case "comment":
                    reportTarget = ReportTarget.Comment;
                    break;
                case "user":
                    reportTarget = ReportTarget.User;
                    break;
                default:
                    return BadRequest("Invalid target type. Must be 'post', 'comment', or 'profile'.");
            }
            return Ok(await _reports.GetReportsByTargetTypeAsync(reportTarget, page, pageSize));
        }
    }
}
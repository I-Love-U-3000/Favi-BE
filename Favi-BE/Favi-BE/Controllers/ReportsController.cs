using Favi_BE.Interfaces;
using Favi_BE.Models.Dtos;
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

        //[Authorize]
        [HttpPost]
        public async Task<ActionResult<ReportResponse>> Create(CreateReportRequest dto) =>
            Ok(await _reports.CreateAsync(dto));

        //[Authorize(Roles = "admin")]
        [HttpGet]
        public async Task<ActionResult<PagedResult<ReportResponse>>> GetAll(int page = 1, int pageSize = 20) =>
            Ok(await _reports.GetAllAsync(page, pageSize));

        //[Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(Guid id, UpdateReportStatusRequest dto)
        {
            var ok = await _reports.UpdateStatusAsync(id, dto);
            return ok ? Ok() : NotFound();
        }
    }
}
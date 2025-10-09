using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using System.Linq;

namespace Favi_BE.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _uow;

        public ReportService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ReportResponse> CreateAsync(CreateReportRequest dto)
        {
            var report = new Report
            {
                Id = Guid.NewGuid(),
                ReporterId = dto.ReporterProfileId,
                TargetType = dto.TargetType,
                TargetId = dto.TargetId,
                Reason = dto.Reason,
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Reports.AddAsync(report);
            await _uow.CompleteAsync();

            return new ReportResponse(report.Id, report.ReporterId, report.TargetType, report.TargetId, report.Reason, report.Status, report.CreatedAt, report.ActedAt, report.Data);
        }

        public async Task<PagedResult<ReportResponse>> GetAllAsync(int page, int pageSize)
        {
            var reports = await _uow.Reports.GetAllAsync();
            var total = reports.Count();
            var paged = reports.Skip((page - 1) * pageSize).Take(pageSize);

            var dtos = paged.Select(r => new ReportResponse(r.Id, r.ReporterId, r.TargetType, r.TargetId, r.Reason?? string.Empty, r.Status, r.CreatedAt, r.ActedAt, r.Data));
            return new PagedResult<ReportResponse>(dtos, page, pageSize, total);
        }

        public async Task<bool> UpdateStatusAsync(Guid reportId, UpdateReportStatusRequest dto)
        {
            var report = await _uow.Reports.GetByIdAsync(reportId);
            if (report is null) return false;

            report.Status = dto.NewStatus;
            report.ActedAt = DateTime.UtcNow;

            _uow.Reports.Update(report);
            await _uow.CompleteAsync();
            return true;
        }

        public async Task<PagedResult<ReportResponse>> GetReportsByReporterIdAsync(Guid reporterId, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            var (reports, total) = await _uow.Reports.GetReportsByReporterIdPagedAsync(reporterId, skip, pageSize);
            var dtos = reports.Select(r => new ReportResponse(r.Id, r.ReporterId, r.TargetType, r.TargetId, r.Reason ?? string.Empty, r.Status, r.CreatedAt, r.ActedAt, r.Data));
            return new PagedResult<ReportResponse>(dtos, page, pageSize, total);
        }
        public async Task<PagedResult<ReportResponse>> GetReportsByTargetTypeAsync(ReportTarget reportTarget, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            var (reports, total) = await _uow.Reports.GetReportsByTargetTypePagedAsync(reportTarget, skip, pageSize);
            var dtos = reports.Select(r => new ReportResponse(r.Id, r.ReporterId, r.TargetType, r.TargetId, r.Reason ?? string.Empty, r.Status, r.CreatedAt, r.ActedAt, r.Data));
            return new PagedResult<ReportResponse>(dtos, page, pageSize, total);
        }
        public async Task<PagedResult<ReportResponse>> GetReportsByTargetIdAsync(Guid targetId, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            var (reports, total) = await _uow.Reports.GetReportsByTargetIdPagedAsync(targetId, skip, pageSize);
            var dtos = reports.Select(r => new ReportResponse(r.Id, r.ReporterId, r.TargetType, r.TargetId, r.Reason ?? string.Empty, r.Status, r.CreatedAt, r.ActedAt, r.Data));
            return new PagedResult<ReportResponse>(dtos, page, pageSize, total);
        }
    }
}

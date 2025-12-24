using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;

namespace Favi_BE.Interfaces.Services
{
    public interface IReportService
    {
        Task<ReportResponse> CreateAsync(CreateReportRequest dto);
        Task<PagedResult<ReportResponse>> GetAllAsync(int page, int pageSize);
        Task<bool> UpdateStatusAsync(Guid reportId, UpdateReportStatusRequest dto, Guid adminId);
        Task<PagedResult<ReportResponse>> GetReportsByReporterIdAsync(Guid reporterId, int page, int pageSize);
        Task<PagedResult<ReportResponse>> GetReportsByTargetIdAsync(Guid targetId, int skip, int take);
        Task<PagedResult<ReportResponse>> GetReportsByTargetTypeAsync(ReportTarget targetType, int skip, int take);
    }

}

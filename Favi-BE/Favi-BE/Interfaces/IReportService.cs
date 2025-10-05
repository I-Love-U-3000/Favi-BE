using Favi_BE.Models.Dtos;

namespace Favi_BE.Interfaces
{
    public interface IReportService
    {
        Task<ReportResponse> CreateAsync(CreateReportRequest dto);
        Task<PagedResult<ReportResponse>> GetAllAsync(int page, int pageSize);
        Task<bool> UpdateStatusAsync(Guid reportId, UpdateReportStatusRequest dto);
    }

}

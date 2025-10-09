using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Repositories
{
    public interface IReportRepository : IGenericRepository<Report>
    {
        Task<IEnumerable<Report>> GetPendingReportsAsync(int skip, int take);
        Task<IEnumerable<Report>> GetReportsByTargetTypeAsync(ReportTarget reportTarget, int skip, int take);
        Task<IEnumerable<Report>> GetReportsByTargetIdAsync(Guid targetId, int skip, int take);
        Task<IEnumerable<Report>> GetReportsByReporterIdAsync(Guid reporterId, int skip, int take);
        Task<(IEnumerable<Report> Items, int Total)> GetReportsByReporterIdPagedAsync(Guid reporterId, int skip, int take);
        Task<(IEnumerable<Report> Items, int Total)> GetReportsByTargetTypePagedAsync(ReportTarget targetType, int skip, int take);
        Task<(IEnumerable<Report> Items, int Total)> GetReportsByTargetIdPagedAsync(Guid targetId, int skip, int take);
    }
}
using Favi_BE.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Favi_BE.Interfaces.Repositories
{
    public interface IReportRepository : IGenericRepository<Report>
    {
        Task<IEnumerable<Report>> GetPendingReportsAsync(int skip, int take);
        Task<IEnumerable<Report>> GetReportsByContentTypeAsync(string contentType, int skip, int take);
    }
}
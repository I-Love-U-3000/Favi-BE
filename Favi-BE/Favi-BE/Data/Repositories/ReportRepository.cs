using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.Data.Repositories
{
    public class ReportRepository : GenericRepository<Report>, IReportRepository
    {
        public ReportRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Report>> GetPendingReportsAsync(int skip, int take)
        {
            return await _dbSet
                .Where(r => r.Status == Models.Enums.ReportStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(r => r.Reporter)
                .ToListAsync();
        }

        public async Task<IEnumerable<Report>> GetReportsByContentTypeAsync(string contentType, int skip, int take)
        {
            return await _dbSet
                .Where(r => r.TargetType.ToString() == contentType)
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(r => r.Reporter)
                .ToListAsync();
        }
    }
}
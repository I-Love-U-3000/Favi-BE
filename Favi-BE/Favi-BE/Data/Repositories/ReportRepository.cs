using Favi_BE.Interfaces.Repositories;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
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

        public async Task<IEnumerable<Report>> GetReportsByTargetTypeAsync(ReportTarget targetType, int skip, int take)
        {
            return await _dbSet
                .Where(r => r.TargetType == targetType)
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(r => r.Reporter)
                .ToListAsync();
        }
        public async Task<IEnumerable<Report>> GetReportsByTargetIdAsync(Guid targetId, int skip, int take)
        {
            return await _dbSet
                .Where(r => r.TargetId == targetId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(r => r.Reporter)
                .ToListAsync();
        }
        public async Task<IEnumerable<Report>> GetReportsByReporterIdAsync(Guid reporterId, int skip, int take)
        {
            return await _dbSet
                .Where(r => r.ReporterId == reporterId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Include(r => r.Reporter)
                .ToListAsync();
        }
        public async Task<(IEnumerable<Report> Items, int Total)> GetReportsByReporterIdPagedAsync(Guid reporterId, int skip, int take)
        {
            var query = _dbSet.Where(r => r.ReporterId == reporterId).OrderByDescending(r => r.CreatedAt);
            var total = await query.CountAsync();
            var items = await query.Skip(skip).Take(take).ToListAsync();
            return (items, total);
        }

        public async Task<(IEnumerable<Report> Items, int Total)> GetReportsByTargetTypePagedAsync(ReportTarget targetType, int skip, int take)
        {
            var query = _dbSet.Where(r => r.TargetType == targetType).OrderByDescending(r => r.CreatedAt);
            var total = await query.CountAsync();
            var items = await query.Skip(skip).Take(take).ToListAsync();
            return (items, total);
        }

        public async Task<(IEnumerable<Report> Items, int Total)> GetReportsByTargetIdPagedAsync(Guid targetId, int skip, int take)
        {
            var query = _dbSet.Where(r => r.TargetId == targetId).OrderByDescending(r => r.CreatedAt);
            var total = await query.CountAsync();
            var items = await query.Skip(skip).Take(take).ToListAsync();
            return (items, total);
        }

    }
}
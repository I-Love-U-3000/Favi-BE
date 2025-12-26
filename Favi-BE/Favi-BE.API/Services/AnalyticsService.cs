using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Favi_BE.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(IUnitOfWork uow, ILogger<AnalyticsService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<DashboardStatsResponse> GetDashboardStatsAsync()
        {
            try
            {
                var allProfiles = await _uow.Profiles.GetAllAsync();
                var allPosts = await _uow.Posts.GetAllAsync();
                var allReports = await _uow.Reports.GetAllAsync();

                var now = DateTime.UtcNow;
                var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

                var totalUsers = allProfiles.Count();
                var totalPosts = allPosts.Count();
                var activeUsers = allProfiles.Count(p => !p.IsBanned && p.LastActiveAt.HasValue && p.LastActiveAt.Value >= now.AddDays(-7));
                var bannedUsers = allProfiles.Count(p => p.IsBanned);
                var pendingReports = allReports.Count(r => r.Status == ReportStatus.Pending);
                var todayPosts = allPosts.Count(p => p.CreatedAt >= todayStart);
                var todayUsers = allProfiles.Count(p => p.CreatedAt >= todayStart);

                return new DashboardStatsResponse(
                    totalUsers,
                    totalPosts,
                    activeUsers,
                    bannedUsers,
                    pendingReports,
                    todayPosts,
                    todayUsers
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                throw;
            }
        }

        public async Task<PagedResult<AnalyticsUserDto>> GetUsersAsync(
            string? search, string? role, string? status, string? sortBy, string? sortOrder, int page, int pageSize)
        {
            try
            {
                var query = await _uow.Profiles.GetAllAsync();
                var queryable = query.AsQueryable();

                // Filter by search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    queryable = queryable.Where(p =>
                        (p.Username != null && p.Username.ToLower().Contains(searchLower)) ||
                        (p.DisplayName != null && p.DisplayName.ToLower().Contains(searchLower)));
                }

                // Filter by role
                if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var userRole))
                {
                    queryable = queryable.Where(p => p.Role == userRole);
                }

                // Filter by status
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (status.ToLower() == "banned")
                    {
                        queryable = queryable.Where(p => p.IsBanned);
                    }
                    else if (status.ToLower() == "active")
                    {
                        queryable = queryable.Where(p => !p.IsBanned);
                    }
                }

                // Sorting
                sortBy = string.IsNullOrWhiteSpace(sortBy) ? "CreatedAt" : sortBy;
                sortOrder = string.IsNullOrWhiteSpace(sortOrder) ? "desc" : sortOrder.ToLower();

                queryable = sortBy.ToLower() switch
                {
                    "username" => sortOrder == "asc" ? queryable.OrderBy(p => p.Username) : queryable.OrderByDescending(p => p.Username),
                    "displayname" => sortOrder == "asc" ? queryable.OrderBy(p => p.DisplayName) : queryable.OrderByDescending(p => p.DisplayName),
                    "lastactiveat" => sortOrder == "asc" ? queryable.OrderBy(p => p.LastActiveAt) : queryable.OrderByDescending(p => p.LastActiveAt),
                    _ => sortOrder == "asc" ? queryable.OrderBy(p => p.CreatedAt) : queryable.OrderByDescending(p => p.CreatedAt)
                };

                var total = queryable.Count();
                var skip = (page - 1) * pageSize;
                var items = queryable.Skip(skip).Take(pageSize).ToList();

                var allPosts = await _uow.Posts.GetAllAsync();
                var postsCountPerUser = allPosts.GroupBy(p => p.ProfileId).ToDictionary(g => g.Key, g => g.Count());

                var allFollows = await _uow.Follows.GetAllAsync();
                var followersCountPerUser = allFollows.GroupBy(f => f.FolloweeId).ToDictionary(g => g.Key, g => g.Count());

                var dtos = items.Select(p => new AnalyticsUserDto(
                    p.Id,
                    p.Username,
                    p.DisplayName,
                    p.AvatarUrl,
                    p.CreatedAt,
                    p.LastActiveAt ?? DateTime.MinValue,
                    p.IsBanned,
                    p.BannedUntil,
                    p.Role,
                    postsCountPerUser.GetValueOrDefault(p.Id, 0),
                    followersCountPerUser.GetValueOrDefault(p.Id, 0)
                ));

                return new PagedResult<AnalyticsUserDto>(dtos, page, pageSize, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users analytics");
                throw;
            }
        }

        public async Task<PagedResult<AnalyticsPostDto>> GetPostsAsync(
            string? search, string? status, string? sortBy, string? sortOrder, int page, int pageSize)
        {
            try
            {
                var query = await _uow.Posts.GetAllAsync();
                var queryable = query.AsQueryable();

                // Filter by search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    queryable = queryable.Where(p =>
                        (p.Caption != null && p.Caption.ToLower().Contains(searchLower)));
                }

                // Filter by status
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (status.ToLower() == "deleted")
                    {
                        queryable = queryable.Where(p => p.DeletedDayExpiredAt.HasValue);
                    }
                    else if (status.ToLower() == "active")
                    {
                        queryable = queryable.Where(p => !p.DeletedDayExpiredAt.HasValue);
                    }
                }

                // Sorting
                sortBy = string.IsNullOrWhiteSpace(sortBy) ? "CreatedAt" : sortBy;
                sortOrder = string.IsNullOrWhiteSpace(sortOrder) ? "desc" : sortOrder.ToLower();

                queryable = sortBy.ToLower() switch
                {
                    "caption" => sortOrder == "asc" ? queryable.OrderBy(p => p.Caption) : queryable.OrderByDescending(p => p.Caption),
                    _ => sortOrder == "asc" ? queryable.OrderBy(p => p.CreatedAt) : queryable.OrderByDescending(p => p.CreatedAt)
                };

                var total = queryable.Count();
                var skip = (page - 1) * pageSize;
                var items = queryable.Skip(skip).Take(pageSize).ToList();

                var allProfiles = await _uow.Profiles.GetAllAsync();
                var profileDict = allProfiles.ToDictionary(p => p.Id, p => p);

                var allComments = await _uow.Comments.GetAllAsync();
                var commentsCountPerPost = allComments.GroupBy(c => c.PostId).ToDictionary(g => g.Key, g => g.Count());

                var allReactions = await _uow.Reactions.GetAllAsync();
                var reactionsCountPerPost = allReactions.GroupBy(r => r.PostId).ToDictionary(g => g.Key, g => g.Count());

                var dtos = items.Select(p => profileDict.TryGetValue(p.ProfileId, out var profile)
                    ? new AnalyticsPostDto(
                        p.Id,
                        p.ProfileId,
                        profile.Username,
                        profile.DisplayName,
                        p.Caption,
                        p.CreatedAt,
                        p.Privacy,
                        commentsCountPerPost.GetValueOrDefault(p.Id, 0),
                        reactionsCountPerPost.GetValueOrDefault(p.Id, 0),
                        p.DeletedDayExpiredAt.HasValue
                    )
                    : new AnalyticsPostDto(
                        p.Id,
                        p.ProfileId,
                        null,
                        null,
                        p.Caption,
                        p.CreatedAt,
                        p.Privacy,
                        commentsCountPerPost.GetValueOrDefault(p.Id, 0),
                        reactionsCountPerPost.GetValueOrDefault(p.Id, 0),
                        p.DeletedDayExpiredAt.HasValue
                    ));

                return new PagedResult<AnalyticsPostDto>(dtos, page, pageSize, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting posts analytics");
                throw;
            }
        }
    }
}

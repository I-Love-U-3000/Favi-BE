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
                var reactionsCountPerPost = allReactions
                    .Where(r => r.PostId.HasValue)
                    .GroupBy(r => r.PostId!.Value)
                    .ToDictionary(g => g.Key, g => g.Count());

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

        // ============================================================
        // CHART METHODS - Growth over time
        // ============================================================

        public async Task<GrowthChartResponse> GetGrowthChartAsync(DateTime? fromDate, DateTime? toDate, string interval = "day")
        {
            try
            {
                var (from, to) = NormalizeDateRange(fromDate, toDate);

                var allProfiles = await _uow.Profiles.GetAllAsync();
                var allPosts = await _uow.Posts.GetAllAsync();
                var allReports = await _uow.Reports.GetAllAsync();

                var users = GroupByInterval(
                    allProfiles.Where(p => p.CreatedAt >= from && p.CreatedAt <= to),
                    p => p.CreatedAt,
                    from, to, interval);

                var posts = GroupByInterval(
                    allPosts.Where(p => p.CreatedAt >= from && p.CreatedAt <= to),
                    p => p.CreatedAt,
                    from, to, interval);

                var reports = GroupByInterval(
                    allReports.Where(r => r.CreatedAt >= from && r.CreatedAt <= to),
                    r => r.CreatedAt,
                    from, to, interval);

                return new GrowthChartResponse(users, posts, reports, from, to, interval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting growth chart");
                throw;
            }
        }

        public async Task<UserActivityChartResponse> GetUserActivityChartAsync(DateTime? fromDate, DateTime? toDate, string interval = "day")
        {
            try
            {
                var (from, to) = NormalizeDateRange(fromDate, toDate);

                var allProfiles = await _uow.Profiles.GetAllAsync();

                var newUsers = GroupByInterval(
                    allProfiles.Where(p => p.CreatedAt >= from && p.CreatedAt <= to),
                    p => p.CreatedAt,
                    from, to, interval);

                // Active users: users who had activity in the period (approximated by LastActiveAt)
                var activeUsers = GroupByInterval(
                    allProfiles.Where(p => p.LastActiveAt.HasValue && p.LastActiveAt.Value >= from && p.LastActiveAt.Value <= to && !p.IsBanned),
                    p => p.LastActiveAt!.Value,
                    from, to, interval);

                // Banned users: count users banned in the period (approximated by current state)
                // For accurate tracking, we would need a ban history table
                var bannedProfiles = allProfiles.Where(p => p.IsBanned).ToList();
                var bannedUsers = GenerateEmptyTimeSeries(from, to, interval)
                    .Select(d => new TimeSeriesDataPoint(d, bannedProfiles.Count))
                    .ToList();

                return new UserActivityChartResponse(newUsers, activeUsers, bannedUsers, from, to, interval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user activity chart");
                throw;
            }
        }

        public async Task<ContentActivityChartResponse> GetContentActivityChartAsync(DateTime? fromDate, DateTime? toDate, string interval = "day")
        {
            try
            {
                var (from, to) = NormalizeDateRange(fromDate, toDate);

                var allPosts = await _uow.Posts.GetAllAsync();
                var allComments = await _uow.Comments.GetAllAsync();
                var allReactions = await _uow.Reactions.GetAllAsync();

                var posts = GroupByInterval(
                    allPosts.Where(p => p.CreatedAt >= from && p.CreatedAt <= to),
                    p => p.CreatedAt,
                    from, to, interval);

                var comments = GroupByInterval(
                    allComments.Where(c => c.CreatedAt >= from && c.CreatedAt <= to),
                    c => c.CreatedAt,
                    from, to, interval);

                var reactions = GroupByInterval(
                    allReactions.Where(r => r.CreatedAt >= from && r.CreatedAt <= to),
                    r => r.CreatedAt,
                    from, to, interval);

                return new ContentActivityChartResponse(posts, comments, reactions, from, to, interval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting content activity chart");
                throw;
            }
        }

        // ============================================================
        // DISTRIBUTION CHARTS
        // ============================================================

        public async Task<UserRoleDistributionResponse> GetUserRoleDistributionAsync()
        {
            try
            {
                var allProfiles = await _uow.Profiles.GetAllAsync();
                var total = allProfiles.Count();

                var roles = allProfiles
                    .GroupBy(p => p.Role)
                    .Select(g => new LabeledDataPoint(
                        g.Key.ToString(),
                        g.Count(),
                        total > 0 ? Math.Round((double)g.Count() / total * 100, 2) : 0
                    ))
                    .OrderByDescending(r => r.Count)
                    .ToList();

                return new UserRoleDistributionResponse(roles, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user role distribution");
                throw;
            }
        }

        public async Task<UserStatusDistributionResponse> GetUserStatusDistributionAsync()
        {
            try
            {
                var allProfiles = await _uow.Profiles.GetAllAsync();
                var now = DateTime.UtcNow;
                var inactiveThreshold = now.AddDays(-30);

                var total = allProfiles.Count();
                var banned = allProfiles.Count(p => p.IsBanned);
                var inactive = allProfiles.Count(p => !p.IsBanned && (!p.LastActiveAt.HasValue || p.LastActiveAt.Value < inactiveThreshold));
                var active = total - banned - inactive;

                return new UserStatusDistributionResponse(active, banned, inactive, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user status distribution");
                throw;
            }
        }

        public async Task<PostPrivacyDistributionResponse> GetPostPrivacyDistributionAsync()
        {
            try
            {
                var allPosts = await _uow.Posts.GetAllAsync();
                var activePosts = allPosts.Where(p => !p.DeletedDayExpiredAt.HasValue).ToList();
                var total = activePosts.Count;

                var privacyLevels = activePosts
                    .GroupBy(p => p.Privacy)
                    .Select(g => new LabeledDataPoint(
                        g.Key.ToString(),
                        g.Count(),
                        total > 0 ? Math.Round((double)g.Count() / total * 100, 2) : 0
                    ))
                    .OrderByDescending(p => p.Count)
                    .ToList();

                return new PostPrivacyDistributionResponse(privacyLevels, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post privacy distribution");
                throw;
            }
        }

        public async Task<ReportStatusDistributionResponse> GetReportStatusDistributionAsync()
        {
            try
            {
                var allReports = await _uow.Reports.GetAllAsync();
                var total = allReports.Count();

                var pending = allReports.Count(r => r.Status == ReportStatus.Pending);
                var resolved = allReports.Count(r => r.Status == ReportStatus.Resolved);
                var rejected = allReports.Count(r => r.Status == ReportStatus.Rejected);

                return new ReportStatusDistributionResponse(pending, resolved, rejected, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report status distribution");
                throw;
            }
        }

        // ============================================================
        // TOP ENTITIES
        // ============================================================

        public async Task<IEnumerable<TopUserDto>> GetTopUsersAsync(int limit = 10)
        {
            try
            {
                var allProfiles = await _uow.Profiles.GetAllAsync();
                var allPosts = await _uow.Posts.GetAllAsync();
                var allFollows = await _uow.Follows.GetAllAsync();
                var allReactions = await _uow.Reactions.GetAllAsync();

                var postsCountPerUser = allPosts
                    .Where(p => !p.DeletedDayExpiredAt.HasValue)
                    .GroupBy(p => p.ProfileId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var followersCountPerUser = allFollows
                    .GroupBy(f => f.FolloweeId)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Reactions received = reactions on user's posts
                var postsByUser = allPosts
                    .Where(p => !p.DeletedDayExpiredAt.HasValue)
                    .GroupBy(p => p.ProfileId)
                    .ToDictionary(g => g.Key, g => g.Select(p => p.Id).ToHashSet());

                var reactionsPerUser = allProfiles.ToDictionary(
                    p => p.Id,
                    p => postsByUser.TryGetValue(p.Id, out var postIds)
                        ? allReactions.Count(r => r.PostId.HasValue && postIds.Contains(r.PostId.Value))
                        : 0
                );

                var topUsers = allProfiles
                    .Where(p => !p.IsBanned)
                    .Select(p => new TopUserDto(
                        p.Id,
                        p.Username,
                        p.DisplayName,
                        p.AvatarUrl,
                        postsCountPerUser.GetValueOrDefault(p.Id, 0),
                        followersCountPerUser.GetValueOrDefault(p.Id, 0),
                        reactionsPerUser.GetValueOrDefault(p.Id, 0)
                    ))
                    .OrderByDescending(u => u.FollowersCount)
                    .ThenByDescending(u => u.ReactionsReceived)
                    .ThenByDescending(u => u.PostsCount)
                    .Take(limit)
                    .ToList();

                return topUsers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top users");
                throw;
            }
        }

        public async Task<IEnumerable<TopPostDto>> GetTopPostsAsync(int limit = 10)
        {
            try
            {
                var allPosts = await _uow.Posts.GetAllAsync();
                var allProfiles = await _uow.Profiles.GetAllAsync();
                var allComments = await _uow.Comments.GetAllAsync();
                var allReactions = await _uow.Reactions.GetAllAsync();

                var profileDict = allProfiles.ToDictionary(p => p.Id, p => p);

                var commentsCountPerPost = allComments
                    .GroupBy(c => c.PostId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var reactionsCountPerPost = allReactions
                    .Where(r => r.PostId.HasValue)
                    .GroupBy(r => r.PostId!.Value)
                    .ToDictionary(g => g.Key, g => g.Count());

                var topPosts = allPosts
                    .Where(p => !p.DeletedDayExpiredAt.HasValue)
                    .Select(p =>
                    {
                        profileDict.TryGetValue(p.ProfileId, out var author);
                        return new TopPostDto(
                            p.Id,
                            p.ProfileId,
                            author?.Username,
                            p.Caption?.Length > 100 ? p.Caption.Substring(0, 100) + "..." : p.Caption,
                            p.CreatedAt,
                            reactionsCountPerPost.GetValueOrDefault(p.Id, 0),
                            commentsCountPerPost.GetValueOrDefault(p.Id, 0)
                        );
                    })
                    .OrderByDescending(p => p.ReactionsCount)
                    .ThenByDescending(p => p.CommentsCount)
                    .Take(limit)
                    .ToList();

                return topPosts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top posts");
                throw;
            }
        }

        // ============================================================
        // PERIOD COMPARISON
        // ============================================================

        public async Task<PeriodComparisonResponse> GetPeriodComparisonAsync(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var (currentFrom, currentTo) = NormalizeDateRange(fromDate, toDate);
                var periodLength = (currentTo - currentFrom).Days;
                var previousFrom = currentFrom.AddDays(-periodLength - 1);
                var previousTo = currentFrom.AddDays(-1);

                var allProfiles = await _uow.Profiles.GetAllAsync();
                var allPosts = await _uow.Posts.GetAllAsync();
                var allComments = await _uow.Comments.GetAllAsync();
                var allReactions = await _uow.Reactions.GetAllAsync();
                var allReports = await _uow.Reports.GetAllAsync();

                var currentPeriod = new PeriodStats(
                    allProfiles.Count(p => p.CreatedAt >= currentFrom && p.CreatedAt <= currentTo),
                    allPosts.Count(p => p.CreatedAt >= currentFrom && p.CreatedAt <= currentTo),
                    allComments.Count(c => c.CreatedAt >= currentFrom && c.CreatedAt <= currentTo),
                    allReactions.Count(r => r.CreatedAt >= currentFrom && r.CreatedAt <= currentTo),
                    allReports.Count(r => r.CreatedAt >= currentFrom && r.CreatedAt <= currentTo),
                    currentFrom,
                    currentTo
                );

                var previousPeriod = new PeriodStats(
                    allProfiles.Count(p => p.CreatedAt >= previousFrom && p.CreatedAt <= previousTo),
                    allPosts.Count(p => p.CreatedAt >= previousFrom && p.CreatedAt <= previousTo),
                    allComments.Count(c => c.CreatedAt >= previousFrom && c.CreatedAt <= previousTo),
                    allReactions.Count(r => r.CreatedAt >= previousFrom && r.CreatedAt <= previousTo),
                    allReports.Count(r => r.CreatedAt >= previousFrom && r.CreatedAt <= previousTo),
                    previousFrom,
                    previousTo
                );

                var growth = new GrowthComparison(
                    CalculateGrowthPercent(previousPeriod.NewUsers, currentPeriod.NewUsers),
                    CalculateGrowthPercent(previousPeriod.NewPosts, currentPeriod.NewPosts),
                    CalculateGrowthPercent(previousPeriod.NewComments, currentPeriod.NewComments),
                    CalculateGrowthPercent(previousPeriod.NewReactions, currentPeriod.NewReactions),
                    CalculateGrowthPercent(previousPeriod.NewReports, currentPeriod.NewReports)
                );

                return new PeriodComparisonResponse(currentPeriod, previousPeriod, growth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting period comparison");
                throw;
            }
        }

        // ============================================================
        // HELPER METHODS
        // ============================================================

        private static (DateTime from, DateTime to) NormalizeDateRange(DateTime? fromDate, DateTime? toDate)
        {
            var now = DateTime.UtcNow;
            var to = toDate.HasValue 
                ? DateTime.SpecifyKind(toDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc)
                : new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, DateTimeKind.Utc);
            var from = fromDate.HasValue 
                ? DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc)
                : to.AddDays(-30).Date;

            return (from, to);
        }

        private static IEnumerable<TimeSeriesDataPoint> GroupByInterval<T>(
            IEnumerable<T> items,
            Func<T, DateTime> dateSelector,
            DateTime from,
            DateTime to,
            string interval)
        {
            var dates = GenerateEmptyTimeSeries(from, to, interval);
            var itemsList = items.ToList();

            return dates.Select(date =>
            {
                var nextDate = GetNextDate(date, interval);
                var count = itemsList.Count(item =>
                {
                    var itemDate = dateSelector(item);
                    return itemDate >= date && itemDate < nextDate;
                });
                return new TimeSeriesDataPoint(date, count);
            });
        }

        private static IEnumerable<DateTime> GenerateEmptyTimeSeries(DateTime from, DateTime to, string interval)
        {
            var dates = new List<DateTime>();
            var current = from.Date;

            while (current <= to)
            {
                dates.Add(current);
                current = GetNextDate(current, interval);
            }

            return dates;
        }

        private static DateTime GetNextDate(DateTime current, string interval)
        {
            return interval.ToLower() switch
            {
                "week" => current.AddDays(7),
                "month" => current.AddMonths(1),
                _ => current.AddDays(1) // default: day
            };
        }

        private static double CalculateGrowthPercent(int previous, int current)
        {
            if (previous == 0)
                return current > 0 ? 100 : 0;

            return Math.Round((double)(current - previous) / previous * 100, 2);
        }
    }
}

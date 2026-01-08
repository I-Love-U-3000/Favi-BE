using Favi_BE.Models.Dtos;

namespace Favi_BE.Interfaces.Services
{
    public interface IAnalyticsService
    {
        Task<DashboardStatsResponse> GetDashboardStatsAsync();
        Task<PagedResult<AnalyticsUserDto>> GetUsersAsync(string? search, string? role, string? status, string? sortBy, string? sortOrder, int page, int pageSize);
        Task<PagedResult<AnalyticsPostDto>> GetPostsAsync(string? search, string? status, string? sortBy, string? sortOrder, int page, int pageSize);
        Task<GrowthChartResponse> GetGrowthChartAsync(DateTime? fromDate, DateTime? toDate, string interval = "day");
        Task<UserActivityChartResponse> GetUserActivityChartAsync(DateTime? fromDate, DateTime? toDate, string interval = "day");
        Task<ContentActivityChartResponse> GetContentActivityChartAsync(DateTime? fromDate, DateTime? toDate, string interval = "day");
        Task<UserRoleDistributionResponse> GetUserRoleDistributionAsync();
        Task<UserStatusDistributionResponse> GetUserStatusDistributionAsync();
        Task<PostPrivacyDistributionResponse> GetPostPrivacyDistributionAsync();
        Task<ReportStatusDistributionResponse> GetReportStatusDistributionAsync();
        Task<IEnumerable<TopUserDto>> GetTopUsersAsync(int limit = 10);
        Task<IEnumerable<TopPostDto>> GetTopPostsAsync(int limit = 10);
        Task<PeriodComparisonResponse> GetPeriodComparisonAsync(DateTime? fromDate, DateTime? toDate);
    }
}

using Favi_BE.Models.Dtos;

namespace Favi_BE.Interfaces.Services
{
    public interface IAnalyticsService
    {
        Task<DashboardStatsResponse> GetDashboardStatsAsync();
        Task<PagedResult<AnalyticsUserDto>> GetUsersAsync(string? search, string? role, string? status, string? sortBy, string? sortOrder, int page, int pageSize);
        Task<PagedResult<AnalyticsPostDto>> GetPostsAsync(string? search, string? status, string? sortBy, string? sortOrder, int page, int pageSize);
    }
}

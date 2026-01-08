using Favi_BE.Authorization;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers;

/// <summary>
/// Admin analytics and charts endpoints
/// </summary>
[ApiController]
[Route("api/admin/analytics")]
[Authorize(Policy = AdminPolicies.RequireAdmin)]
public class AdminAnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AdminAnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    // ============================================================
    // DASHBOARD STATS
    // ============================================================

    /// <summary>
    /// Get dashboard overview statistics
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardStatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardStatsResponse>> GetDashboardStats()
    {
        var stats = await _analyticsService.GetDashboardStatsAsync();
        return Ok(stats);
    }

    /// <summary>
    /// Get paginated list of users with analytics data
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<AnalyticsUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AnalyticsUserDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortOrder,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _analyticsService.GetUsersAsync(search, role, status, sortBy, sortOrder, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get paginated list of posts with analytics data
    /// </summary>
    [HttpGet("posts")]
    [ProducesResponseType(typeof(PagedResult<AnalyticsPostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AnalyticsPostDto>>> GetPosts(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortOrder,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _analyticsService.GetPostsAsync(search, status, sortBy, sortOrder, page, pageSize);
        return Ok(result);
    }

    // ============================================================
    // TIME SERIES CHARTS
    // ============================================================

    /// <summary>
    /// Get growth chart data (users, posts, reports over time)
    /// </summary>
    /// <param name="fromDate">Start date (default: 30 days ago)</param>
    /// <param name="toDate">End date (default: today)</param>
    /// <param name="interval">Grouping interval: day, week, month (default: day)</param>
    [HttpGet("charts/growth")]
    [ProducesResponseType(typeof(GrowthChartResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GrowthChartResponse>> GetGrowthChart(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string interval = "day")
    {
        var validIntervals = new[] { "day", "week", "month" };
        if (!validIntervals.Contains(interval.ToLower()))
            interval = "day";

        var result = await _analyticsService.GetGrowthChartAsync(fromDate, toDate, interval);
        return Ok(result);
    }

    /// <summary>
    /// Get user activity chart (new users, active users, banned users over time)
    /// </summary>
    [HttpGet("charts/user-activity")]
    [ProducesResponseType(typeof(UserActivityChartResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserActivityChartResponse>> GetUserActivityChart(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string interval = "day")
    {
        var validIntervals = new[] { "day", "week", "month" };
        if (!validIntervals.Contains(interval.ToLower()))
            interval = "day";

        var result = await _analyticsService.GetUserActivityChartAsync(fromDate, toDate, interval);
        return Ok(result);
    }

    /// <summary>
    /// Get content activity chart (posts, comments, reactions over time)
    /// </summary>
    [HttpGet("charts/content-activity")]
    [ProducesResponseType(typeof(ContentActivityChartResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ContentActivityChartResponse>> GetContentActivityChart(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string interval = "day")
    {
        var validIntervals = new[] { "day", "week", "month" };
        if (!validIntervals.Contains(interval.ToLower()))
            interval = "day";

        var result = await _analyticsService.GetContentActivityChartAsync(fromDate, toDate, interval);
        return Ok(result);
    }

    // ============================================================
    // DISTRIBUTION CHARTS
    // ============================================================

    /// <summary>
    /// Get user distribution by role (for pie/donut chart)
    /// </summary>
    [HttpGet("charts/user-roles")]
    [ProducesResponseType(typeof(UserRoleDistributionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserRoleDistributionResponse>> GetUserRoleDistribution()
    {
        var result = await _analyticsService.GetUserRoleDistributionAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get user distribution by status (active, banned, inactive)
    /// </summary>
    [HttpGet("charts/user-status")]
    [ProducesResponseType(typeof(UserStatusDistributionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserStatusDistributionResponse>> GetUserStatusDistribution()
    {
        var result = await _analyticsService.GetUserStatusDistributionAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get post distribution by privacy level
    /// </summary>
    [HttpGet("charts/post-privacy")]
    [ProducesResponseType(typeof(PostPrivacyDistributionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PostPrivacyDistributionResponse>> GetPostPrivacyDistribution()
    {
        var result = await _analyticsService.GetPostPrivacyDistributionAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get report distribution by status
    /// </summary>
    [HttpGet("charts/report-status")]
    [ProducesResponseType(typeof(ReportStatusDistributionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportStatusDistributionResponse>> GetReportStatusDistribution()
    {
        var result = await _analyticsService.GetReportStatusDistributionAsync();
        return Ok(result);
    }

    // ============================================================
    // TOP ENTITIES
    // ============================================================

    /// <summary>
    /// Get top users by engagement (followers, reactions received)
    /// </summary>
    [HttpGet("top-users")]
    [ProducesResponseType(typeof(IEnumerable<TopUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TopUserDto>>> GetTopUsers([FromQuery] int limit = 10)
    {
        if (limit < 1) limit = 10;
        if (limit > 50) limit = 50;

        var result = await _analyticsService.GetTopUsersAsync(limit);
        return Ok(result);
    }

    /// <summary>
    /// Get top posts by engagement (reactions, comments)
    /// </summary>
    [HttpGet("top-posts")]
    [ProducesResponseType(typeof(IEnumerable<TopPostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TopPostDto>>> GetTopPosts([FromQuery] int limit = 10)
    {
        if (limit < 1) limit = 10;
        if (limit > 50) limit = 50;

        var result = await _analyticsService.GetTopPostsAsync(limit);
        return Ok(result);
    }

    // ============================================================
    // PERIOD COMPARISON
    // ============================================================

    /// <summary>
    /// Get period comparison (current vs previous period)
    /// </summary>
    /// <param name="fromDate">Start of current period (default: 30 days ago)</param>
    /// <param name="toDate">End of current period (default: today)</param>
    [HttpGet("comparison")]
    [ProducesResponseType(typeof(PeriodComparisonResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PeriodComparisonResponse>> GetPeriodComparison(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _analyticsService.GetPeriodComparisonAsync(fromDate, toDate);
        return Ok(result);
    }
}

namespace Favi_BE.Models.Dtos;

/// <summary>
/// A single data point in a time series
/// </summary>
public record TimeSeriesDataPoint(
    DateTime Date,
    int Count
);

/// <summary>
/// A data point with a label (for pie/bar charts)
/// </summary>
public record LabeledDataPoint(
    string Label,
    int Count,
    double? Percentage = null
);

/// <summary>
/// Growth chart response - shows entity counts over time
/// </summary>
public record GrowthChartResponse(
    IEnumerable<TimeSeriesDataPoint> Users,
    IEnumerable<TimeSeriesDataPoint> Posts,
    IEnumerable<TimeSeriesDataPoint> Reports,
    DateTime FromDate,
    DateTime ToDate,
    string Interval // "day", "week", "month"
);

/// <summary>
/// User activity chart response
/// </summary>
public record UserActivityChartResponse(
    IEnumerable<TimeSeriesDataPoint> NewUsers,
    IEnumerable<TimeSeriesDataPoint> ActiveUsers,
    IEnumerable<TimeSeriesDataPoint> BannedUsers,
    DateTime FromDate,
    DateTime ToDate,
    string Interval
);

/// <summary>
/// Content activity chart response
/// </summary>
public record ContentActivityChartResponse(
    IEnumerable<TimeSeriesDataPoint> Posts,
    IEnumerable<TimeSeriesDataPoint> Comments,
    IEnumerable<TimeSeriesDataPoint> Reactions,
    DateTime FromDate,
    DateTime ToDate,
    string Interval
);

/// <summary>
/// User distribution by role
/// </summary>
public record UserRoleDistributionResponse(
    IEnumerable<LabeledDataPoint> Roles,
    int TotalUsers
);

/// <summary>
/// User status distribution (active, banned, inactive)
/// </summary>
public record UserStatusDistributionResponse(
    int ActiveUsers,
    int BannedUsers,
    int InactiveUsers,
    int TotalUsers
);

/// <summary>
/// Post privacy distribution
/// </summary>
public record PostPrivacyDistributionResponse(
    IEnumerable<LabeledDataPoint> PrivacyLevels,
    int TotalPosts
);

/// <summary>
/// Report status distribution
/// </summary>
public record ReportStatusDistributionResponse(
    int Pending,
    int Resolved,
    int Rejected,
    int TotalReports
);

/// <summary>
/// Top users by engagement (posts, followers, reactions)
/// </summary>
public record TopUserDto(
    Guid Id,
    string? Username,
    string? DisplayName,
    string? AvatarUrl,
    int PostsCount,
    int FollowersCount,
    int ReactionsReceived
);

/// <summary>
/// Top posts by engagement
/// </summary>
public record TopPostDto(
    Guid Id,
    Guid AuthorId,
    string? AuthorUsername,
    string? Caption,
    DateTime CreatedAt,
    int ReactionsCount,
    int CommentsCount
);

/// <summary>
/// Overview statistics for a time period
/// </summary>
public record PeriodComparisonResponse(
    PeriodStats CurrentPeriod,
    PeriodStats PreviousPeriod,
    GrowthComparison Growth
);

public record PeriodStats(
    int NewUsers,
    int NewPosts,
    int NewComments,
    int NewReactions,
    int NewReports,
    DateTime FromDate,
    DateTime ToDate
);

public record GrowthComparison(
    double UsersGrowthPercent,
    double PostsGrowthPercent,
    double CommentsGrowthPercent,
    double ReactionsGrowthPercent,
    double ReportsGrowthPercent
);

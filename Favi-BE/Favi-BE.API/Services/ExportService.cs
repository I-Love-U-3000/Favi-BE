using System.Globalization;
using System.Text;
using System.Text.Json;
using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Favi_BE.Services;

public class ExportService : IExportService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ExportService> _logger;

    private const int MaxExportRows = 10000;

    public ExportService(IUnitOfWork uow, ILogger<ExportService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    // ============================================================
    // GET DATA FOR EXPORT
    // ============================================================

    public async Task<IEnumerable<ExportUserDto>> GetUsersForExportAsync(ExportUsersRequest request)
    {
        try
        {
            var allProfiles = await _uow.Profiles.GetAllAsync();
            var query = allProfiles.AsQueryable();

            // Filter by search
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchLower = request.Search.ToLower();
                query = query.Where(p =>
                    (p.Username != null && p.Username.ToLower().Contains(searchLower)) ||
                    (p.DisplayName != null && p.DisplayName.ToLower().Contains(searchLower)));
            }

            // Filter by role
            if (!string.IsNullOrWhiteSpace(request.Role) && Enum.TryParse<UserRole>(request.Role, true, out var role))
            {
                query = query.Where(p => p.Role == role);
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (request.Status.ToLower() == "banned")
                    query = query.Where(p => p.IsBanned);
                else if (request.Status.ToLower() == "active")
                    query = query.Where(p => !p.IsBanned);
            }

            // Filter by date
            if (request.FromDate.HasValue)
                query = query.Where(p => p.CreatedAt >= request.FromDate.Value);
            if (request.ToDate.HasValue)
                query = query.Where(p => p.CreatedAt <= request.ToDate.Value);

            var profiles = query.Take(MaxExportRows).ToList();

            // Get additional data
            var allPosts = await _uow.Posts.GetAllAsync();
            var allFollows = await _uow.Follows.GetAllAsync();

            var postsCount = allPosts
                .Where(p => !p.DeletedDayExpiredAt.HasValue)
                .GroupBy(p => p.ProfileId)
                .ToDictionary(g => g.Key, g => g.Count());

            var followersCount = allFollows
                .GroupBy(f => f.FolloweeId)
                .ToDictionary(g => g.Key, g => g.Count());

            var followingCount = allFollows
                .GroupBy(f => f.FollowerId)
                .ToDictionary(g => g.Key, g => g.Count());

            return profiles.Select(p => new ExportUserDto(
                p.Id,
                p.Username,
                p.DisplayName,
                null, // Email not exposed
                p.Role.ToString(),
                p.IsBanned,
                p.BannedUntil,
                p.CreatedAt,
                p.LastActiveAt,
                postsCount.GetValueOrDefault(p.Id, 0),
                followersCount.GetValueOrDefault(p.Id, 0),
                followingCount.GetValueOrDefault(p.Id, 0)
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for export");
            throw;
        }
    }

    public async Task<IEnumerable<ExportPostDto>> GetPostsForExportAsync(ExportPostsRequest request)
    {
        try
        {
            var allPosts = await _uow.Posts.GetAllAsync();
            var query = allPosts.AsQueryable();

            // Filter by search
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchLower = request.Search.ToLower();
                query = query.Where(p => p.Caption != null && p.Caption.ToLower().Contains(searchLower));
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (request.Status.ToLower() == "deleted")
                    query = query.Where(p => p.DeletedDayExpiredAt.HasValue);
                else if (request.Status.ToLower() == "active")
                    query = query.Where(p => !p.DeletedDayExpiredAt.HasValue);
            }

            // Filter by date
            if (request.FromDate.HasValue)
                query = query.Where(p => p.CreatedAt >= request.FromDate.Value);
            if (request.ToDate.HasValue)
                query = query.Where(p => p.CreatedAt <= request.ToDate.Value);

            var posts = query.OrderByDescending(p => p.CreatedAt).Take(MaxExportRows).ToList();

            // Get additional data
            var allProfiles = await _uow.Profiles.GetAllAsync();
            var allReactions = await _uow.Reactions.GetAllAsync();
            var allComments = await _uow.Comments.GetAllAsync();
            var allMedia = await _uow.PostMedia.GetAllAsync();

            var profileDict = allProfiles.ToDictionary(p => p.Id, p => p);
            var reactionsCount = allReactions
                .Where(r => r.PostId.HasValue)
                .GroupBy(r => r.PostId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());
            var commentsCount = allComments
                .GroupBy(c => c.PostId)
                .ToDictionary(g => g.Key, g => g.Count());
            var mediaCount = allMedia
                .GroupBy(m => m.PostId)
                .ToDictionary(g => g.Key, g => g.Count());

            return posts.Select(p =>
            {
                profileDict.TryGetValue(p.ProfileId, out var author);
                return new ExportPostDto(
                    p.Id,
                    p.ProfileId,
                    author?.Username,
                    p.Caption?.Length > 200 ? p.Caption.Substring(0, 200) + "..." : p.Caption,
                    p.Privacy.ToString(),
                    p.CreatedAt,
                    p.DeletedDayExpiredAt.HasValue,
                    reactionsCount.GetValueOrDefault(p.Id, 0),
                    commentsCount.GetValueOrDefault(p.Id, 0),
                    mediaCount.GetValueOrDefault(p.Id, 0)
                );
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts for export");
            throw;
        }
    }

    public async Task<IEnumerable<ExportReportDto>> GetReportsForExportAsync(ExportReportsRequest request)
    {
        try
        {
            var allReports = await _uow.Reports.GetAllAsync();
            var query = allReports.AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<ReportStatus>(request.Status, true, out var status))
            {
                query = query.Where(r => r.Status == status);
            }

            // Filter by target type
            if (!string.IsNullOrWhiteSpace(request.TargetType) && Enum.TryParse<ReportTarget>(request.TargetType, true, out var targetType))
            {
                query = query.Where(r => r.TargetType == targetType);
            }

            // Filter by date
            if (request.FromDate.HasValue)
                query = query.Where(r => r.CreatedAt >= request.FromDate.Value);
            if (request.ToDate.HasValue)
                query = query.Where(r => r.CreatedAt <= request.ToDate.Value);

            var reports = query.OrderByDescending(r => r.CreatedAt).Take(MaxExportRows).ToList();

            // Get reporter usernames
            var allProfiles = await _uow.Profiles.GetAllAsync();
            var profileDict = allProfiles.ToDictionary(p => p.Id, p => p);

            return reports.Select(r =>
            {
                profileDict.TryGetValue(r.ReporterId, out var reporter);
                return new ExportReportDto(
                    r.Id,
                    r.ReporterId,
                    reporter?.Username,
                    r.TargetType.ToString(),
                    r.TargetId,
                    r.Reason ?? string.Empty,
                    r.Status.ToString(),
                    r.CreatedAt,
                    r.ActedAt
                );
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports for export");
            throw;
        }
    }

    public async Task<IEnumerable<ExportAuditLogDto>> GetAuditLogsForExportAsync(ExportAuditLogsRequest request)
    {
        try
        {
            var allActions = await _uow.AdminActions.GetAllAsync();
            var query = allActions.AsQueryable();

            // Filter by action type
            if (!string.IsNullOrWhiteSpace(request.ActionType) && Enum.TryParse<AdminActionType>(request.ActionType, true, out var actionType))
            {
                query = query.Where(a => a.ActionType == actionType);
            }

            // Filter by admin
            if (request.AdminId.HasValue)
            {
                query = query.Where(a => a.AdminId == request.AdminId.Value);
            }

            // Filter by date
            if (request.FromDate.HasValue)
                query = query.Where(a => a.CreatedAt >= request.FromDate.Value);
            if (request.ToDate.HasValue)
                query = query.Where(a => a.CreatedAt <= request.ToDate.Value);

            var actions = query.OrderByDescending(a => a.CreatedAt).Take(MaxExportRows).ToList();

            // Get usernames
            var allProfiles = await _uow.Profiles.GetAllAsync();
            var profileDict = allProfiles.ToDictionary(p => p.Id, p => p);

            return actions.Select(a =>
            {
                profileDict.TryGetValue(a.AdminId, out var admin);
                Profile? target = null;
                if (a.TargetProfileId.HasValue)
                    profileDict.TryGetValue(a.TargetProfileId.Value, out target);

                return new ExportAuditLogDto(
                    a.Id,
                    a.AdminId,
                    admin?.Username,
                    a.ActionType.ToString(),
                    a.TargetProfileId,
                    target?.Username,
                    a.TargetEntityType,
                    a.TargetEntityId,
                    a.Notes,
                    a.CreatedAt
                );
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs for export");
            throw;
        }
    }

    // ============================================================
    // GENERATE FILE CONTENT
    // ============================================================

    public byte[] GenerateCsv<T>(IEnumerable<T> data, string[] headers)
    {
        var sb = new StringBuilder();

        // Add BOM for UTF-8 Excel compatibility
        sb.Append('\uFEFF');

        // Header row
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

        // Data rows
        var properties = typeof(T).GetProperties();
        foreach (var item in data)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);
                return FormatCsvValue(value);
            });
            sb.AppendLine(string.Join(",", values));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public byte[] GenerateJson<T>(IEnumerable<T> data)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var json = JsonSerializer.Serialize(data, options);
        return Encoding.UTF8.GetBytes(json);
    }

    public byte[] GenerateExcel<T>(IEnumerable<T> data, string sheetName, string[] headers)
    {
        // Simple XML-based Excel (SpreadsheetML) without external dependencies
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
        sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
        sb.AppendLine("<Styles>");
        sb.AppendLine("  <Style ss:ID=\"Header\"><Font ss:Bold=\"1\"/><Interior ss:Color=\"#CCCCCC\" ss:Pattern=\"Solid\"/></Style>");
        sb.AppendLine("  <Style ss:ID=\"Date\"><NumberFormat ss:Format=\"yyyy-mm-dd hh:mm:ss\"/></Style>");
        sb.AppendLine("</Styles>");
        sb.AppendLine($"<Worksheet ss:Name=\"{EscapeXml(sheetName)}\">");
        sb.AppendLine("<Table>");

        // Header row
        sb.AppendLine("<Row>");
        foreach (var header in headers)
        {
            sb.AppendLine($"  <Cell ss:StyleID=\"Header\"><Data ss:Type=\"String\">{EscapeXml(header)}</Data></Cell>");
        }
        sb.AppendLine("</Row>");

        // Data rows
        var properties = typeof(T).GetProperties();
        foreach (var item in data)
        {
            sb.AppendLine("<Row>");
            foreach (var prop in properties)
            {
                var value = prop.GetValue(item);
                var (type, formattedValue, styleId) = FormatExcelValue(value);
                var styleAttr = string.IsNullOrEmpty(styleId) ? "" : $" ss:StyleID=\"{styleId}\"";
                sb.AppendLine($"  <Cell{styleAttr}><Data ss:Type=\"{type}\">{EscapeXml(formattedValue)}</Data></Cell>");
            }
            sb.AppendLine("</Row>");
        }

        sb.AppendLine("</Table>");
        sb.AppendLine("</Worksheet>");
        sb.AppendLine("</Workbook>");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ============================================================
    // HELPER METHODS
    // ============================================================

    private static string FormatCsvValue(object? value)
    {
        if (value == null) return "";

        return value switch
        {
            DateTime dt => EscapeCsvField(dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
            bool b => b ? "Yes" : "No",
            Guid g => EscapeCsvField(g.ToString()),
            _ => EscapeCsvField(value.ToString() ?? "")
        };
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";

        // Escape if contains comma, quote, or newline
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    private static (string Type, string Value, string? StyleId) FormatExcelValue(object? value)
    {
        if (value == null) return ("String", "", null);

        return value switch
        {
            DateTime dt => ("DateTime", dt.ToString("yyyy-MM-ddTHH:mm:ss"), "Date"),
            int or long or decimal or double or float => ("Number", value.ToString() ?? "0", null),
            bool b => ("String", b ? "Yes" : "No", null),
            Guid g => ("String", g.ToString(), null),
            _ => ("String", value.ToString() ?? "", null)
        };
    }

    private static string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}

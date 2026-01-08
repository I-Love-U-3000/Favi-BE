using Favi_BE.Models.Dtos;

namespace Favi_BE.Interfaces.Services;

public interface IExportService
{
    // Get data for export
    Task<IEnumerable<ExportUserDto>> GetUsersForExportAsync(ExportUsersRequest request);
    Task<IEnumerable<ExportPostDto>> GetPostsForExportAsync(ExportPostsRequest request);
    Task<IEnumerable<ExportReportDto>> GetReportsForExportAsync(ExportReportsRequest request);
    Task<IEnumerable<ExportAuditLogDto>> GetAuditLogsForExportAsync(ExportAuditLogsRequest request);

    // Generate file content
    byte[] GenerateCsv<T>(IEnumerable<T> data, string[] headers);
    byte[] GenerateJson<T>(IEnumerable<T> data);
    byte[] GenerateExcel<T>(IEnumerable<T> data, string sheetName, string[] headers);
}

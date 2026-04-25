namespace Favi_BE.Modules.Engagement.Application.Responses;

public sealed record EngagementCommandResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static EngagementCommandResult Success() =>
        new() { IsSuccess = true };

    public static EngagementCommandResult Fail(string code, string message) =>
        new() { IsSuccess = false, ErrorCode = code, ErrorMessage = message };
}

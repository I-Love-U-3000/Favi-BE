using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Engagement.Application.Responses;

public sealed record CommentCommandResult
{
    public bool IsSuccess { get; init; }
    public CommentQueryDto? Comment { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static CommentCommandResult Success(CommentQueryDto comment) =>
        new() { IsSuccess = true, Comment = comment };

    public static CommentCommandResult Fail(string code, string message) =>
        new() { IsSuccess = false, ErrorCode = code, ErrorMessage = message };
}

using Favi_BE.Modules.Engagement.Domain;

namespace Favi_BE.Modules.Engagement.Application.Responses;

public sealed record ReactionCommandResult
{
    public bool IsSuccess { get; init; }
    public bool Removed { get; init; }
    public ReactionType? Type { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static ReactionCommandResult Added(ReactionType type) =>
        new() { IsSuccess = true, Removed = false, Type = type };

    public static ReactionCommandResult Changed(ReactionType type) =>
        new() { IsSuccess = true, Removed = false, Type = type };

    public static ReactionCommandResult RemovedReaction() =>
        new() { IsSuccess = true, Removed = true };

    public static ReactionCommandResult Fail(string code, string message) =>
        new() { IsSuccess = false, ErrorCode = code, ErrorMessage = message };
}

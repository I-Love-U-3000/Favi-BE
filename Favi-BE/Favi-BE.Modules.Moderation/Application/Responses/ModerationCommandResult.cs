namespace Favi_BE.Modules.Moderation.Application.Responses;

public sealed class ModerationCommandResult
{
    public bool Succeeded { get; private init; }
    public string? ErrorCode { get; private init; }

    public static ModerationCommandResult Success() => new() { Succeeded = true };
    public static ModerationCommandResult Fail(string errorCode) => new() { Succeeded = false, ErrorCode = errorCode };
}

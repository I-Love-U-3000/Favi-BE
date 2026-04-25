namespace Favi_BE.Modules.SocialGraph.Application.Responses;

public sealed class FollowCommandResult
{
    public bool Succeeded { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static FollowCommandResult Success() => new() { Succeeded = true };
    public static FollowCommandResult Fail(string code, string message) =>
        new() { Succeeded = false, ErrorCode = code, ErrorMessage = message };
}

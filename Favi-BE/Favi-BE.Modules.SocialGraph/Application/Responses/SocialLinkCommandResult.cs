using Favi_BE.Modules.SocialGraph.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.SocialGraph.Application.Responses;

public sealed class SocialLinkCommandResult
{
    public bool Succeeded { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }
    public SocialLinkQueryDto? Data { get; private init; }

    public static SocialLinkCommandResult Success(SocialLinkQueryDto data) =>
        new() { Succeeded = true, Data = data };

    public static SocialLinkCommandResult Success() => new() { Succeeded = true };

    public static SocialLinkCommandResult Fail(string code, string message) =>
        new() { Succeeded = false, ErrorCode = code, ErrorMessage = message };
}

namespace Favi_BE.Modules.Auth.Application.Responses;

public sealed record AuthCommandResult
{
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public string? Message { get; init; }
    public AuthCommandError? Error { get; init; }
    public bool IsSuccess => Error is null;

    public static AuthCommandResult Success(string accessToken, string refreshToken, string message) =>
        new() { AccessToken = accessToken, RefreshToken = refreshToken, Message = message };

    public static AuthCommandResult OkNoTokens(string message) =>
        new() { Message = message };

    public static AuthCommandResult Fail(string code, string message) =>
        new() { Error = new AuthCommandError(code, message) };
}

public sealed record AuthCommandError(string Code, string Message);

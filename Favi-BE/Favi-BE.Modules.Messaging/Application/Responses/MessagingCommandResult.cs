namespace Favi_BE.Modules.Messaging.Application.Responses;

public sealed class MessagingCommandResult
{
    public bool Succeeded { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static MessagingCommandResult Success() => new() { Succeeded = true };
    public static MessagingCommandResult Fail(string code, string message) =>
        new() { Succeeded = false, ErrorCode = code, ErrorMessage = message };
}

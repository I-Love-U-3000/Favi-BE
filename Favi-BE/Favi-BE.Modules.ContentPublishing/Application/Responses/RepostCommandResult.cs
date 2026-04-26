namespace Favi_BE.Modules.ContentPublishing.Application.Responses;

public sealed class RepostCommandResult
{
    public bool Success { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public Guid? RepostId { get; }

    private RepostCommandResult(bool success, string? errorCode, string? errorMessage, Guid? repostId)
    {
        Success = success;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        RepostId = repostId;
    }

    public static RepostCommandResult Ok(Guid repostId) => new(true, null, null, repostId);
    public static RepostCommandResult Ok() => new(true, null, null, null);
    public static RepostCommandResult Fail(string code, string message) => new(false, code, message, null);
}

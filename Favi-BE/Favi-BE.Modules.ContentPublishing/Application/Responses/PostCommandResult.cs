namespace Favi_BE.Modules.ContentPublishing.Application.Responses;

public sealed class PostCommandResult
{
    public bool Success { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public Guid? PostId { get; }

    private PostCommandResult(bool success, string? errorCode, string? errorMessage, Guid? postId)
    {
        Success = success;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        PostId = postId;
    }

    public static PostCommandResult Ok(Guid postId) => new(true, null, null, postId);
    public static PostCommandResult Ok() => new(true, null, null, null);
    public static PostCommandResult Fail(string code, string message) => new(false, code, message, null);
}

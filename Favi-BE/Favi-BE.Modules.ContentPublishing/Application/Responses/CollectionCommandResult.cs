namespace Favi_BE.Modules.ContentPublishing.Application.Responses;

public sealed class CollectionCommandResult
{
    public bool Success { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public Guid? CollectionId { get; }

    private CollectionCommandResult(bool success, string? errorCode, string? errorMessage, Guid? collectionId)
    {
        Success = success;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        CollectionId = collectionId;
    }

    public static CollectionCommandResult Ok(Guid collectionId) => new(true, null, null, collectionId);
    public static CollectionCommandResult Ok() => new(true, null, null, null);
    public static CollectionCommandResult Fail(string code, string message) => new(false, code, message, null);
}

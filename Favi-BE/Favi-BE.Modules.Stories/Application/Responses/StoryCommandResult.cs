namespace Favi_BE.Modules.Stories.Application.Responses;

public record StoryCommandResult(bool Success, Guid StoryId, string? ErrorCode, string? MediaPublicId = null)
{
    public static StoryCommandResult Ok(Guid id, string? mediaPublicId = null) => new(true, id, null, mediaPublicId);
    public static StoryCommandResult Fail(string code) => new(false, Guid.Empty, code, null);
}

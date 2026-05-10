namespace Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

public sealed record RepostReadModel(
    Guid Id,
    Guid ProfileId,
    string Username,
    string? DisplayName,
    string? AvatarUrl,
    Guid OriginalPostId,
    string? OriginalCaption,
    Guid OriginalAuthorProfileId,
    string OriginalAuthorUsername,
    string? OriginalAuthorDisplayName,
    string? OriginalAuthorAvatarUrl,
    IReadOnlyList<PostMediaReadModel> OriginalPostMedias,
    string? Caption,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int CommentsCount,
    int RepostsCount,
    bool IsRepostedByCurrentUser);

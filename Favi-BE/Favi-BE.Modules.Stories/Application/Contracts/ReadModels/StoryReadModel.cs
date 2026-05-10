using Favi_BE.Modules.Stories.Domain;

namespace Favi_BE.Modules.Stories.Application.Contracts.ReadModels;

public sealed record StoryReadModel(
    Guid Id,
    Guid ProfileId,
    string ProfileUsername,
    string? ProfileAvatarUrl,
    string MediaUrl,
    string? ThumbnailUrl,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    StoryPrivacy Privacy,
    bool IsArchived,
    bool IsNSFW,
    int ViewCount,
    bool HasViewed);

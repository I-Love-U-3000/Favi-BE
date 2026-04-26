using Favi_BE.Modules.Stories.Domain;

namespace Favi_BE.Modules.Stories.Application.Contracts.WriteModels;

public record StoryWriteData(
    Guid Id,
    Guid ProfileId,
    string? MediaUrl,
    string? MediaPublicId,
    int MediaWidth,
    int MediaHeight,
    string? MediaFormat,
    string? ThumbnailUrl,
    StoryPrivacy Privacy,
    bool IsArchived,
    bool IsNSFW,
    DateTime CreatedAt,
    DateTime ExpiresAt
);

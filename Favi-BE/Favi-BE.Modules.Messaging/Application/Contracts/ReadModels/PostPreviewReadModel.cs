namespace Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;

public sealed record PostPreviewReadModel(
    Guid Id,
    Guid AuthorProfileId,
    string? Caption,
    string? ThumbnailUrl,
    int MediasCount,
    DateTime CreatedAt);

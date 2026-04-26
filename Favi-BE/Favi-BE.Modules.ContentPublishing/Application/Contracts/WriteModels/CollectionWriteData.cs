using Favi_BE.Modules.ContentPublishing.Domain;

namespace Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels;

public sealed record CollectionWriteData(
    Guid Id,
    Guid ProfileId,
    string Title,
    string? Description,
    string? CoverImageUrl,
    string? CoverImagePublicId,
    CollectionPrivacy Privacy,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

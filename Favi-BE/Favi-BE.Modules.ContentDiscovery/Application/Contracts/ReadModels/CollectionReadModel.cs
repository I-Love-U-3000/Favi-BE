namespace Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

public sealed record CollectionReadModel(
    Guid Id,
    Guid OwnerProfileId,
    string Title,
    string? Description,
    string? CoverImageUrl,
    int Privacy,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<Guid> PostIds,
    int PostCount);

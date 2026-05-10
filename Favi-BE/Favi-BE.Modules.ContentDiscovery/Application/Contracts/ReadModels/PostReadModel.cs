namespace Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

public sealed record PostReadModel(
    Guid Id,
    Guid AuthorProfileId,
    string? Caption,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int Privacy,
    IReadOnlyList<PostMediaReadModel> Medias,
    IReadOnlyList<TagReadModel> Tags,
    PostLocationReadModel? Location,
    bool IsNSFW,
    int CommentsCount);

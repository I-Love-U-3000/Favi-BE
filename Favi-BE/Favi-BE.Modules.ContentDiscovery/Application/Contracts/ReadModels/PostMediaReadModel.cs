namespace Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

public sealed record PostMediaReadModel(
    Guid Id,
    Guid PostId,
    string Url,
    string? PublicId,
    int Width,
    int Height,
    string? Format,
    int Position,
    string? ThumbnailUrl);

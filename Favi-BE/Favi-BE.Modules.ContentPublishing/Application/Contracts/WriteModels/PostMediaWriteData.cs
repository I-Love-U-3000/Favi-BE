namespace Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels;

public sealed record PostMediaWriteData(
    Guid Id,
    Guid PostId,
    string Url,
    string? ThumbnailUrl,
    string PublicId,
    int Width,
    int Height,
    string Format,
    int Position
);

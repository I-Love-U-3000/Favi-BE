namespace Favi_BE.Modules.Auth.Application.Contracts.WriteModels;

/// <summary>
/// Pre-uploaded image data passed from API layer into avatar/poster commands.
/// The module never touches IFormFile or HTTP I/O directly.
/// </summary>
public sealed record UploadedImageData(
    string Url,
    string? ThumbnailUrl,
    string PublicId,
    int Width,
    int Height,
    string Format
);

namespace Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels;

/// <summary>
/// Pre-uploaded media data passed from controller (after Cloudinary upload) into commands.
/// The module never touches IFormFile or HTTP I/O directly.
/// </summary>
public sealed record UploadedMediaItem(
    string Url,
    string? ThumbnailUrl,
    string PublicId,
    int Width,
    int Height,
    string Format
);

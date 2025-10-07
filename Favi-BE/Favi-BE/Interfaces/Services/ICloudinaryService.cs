namespace Favi_BE.Interfaces.Services
{
    public interface ICloudinaryService
    {
        Task<(string Url, string? ThumbnailUrl, string PublicId, int Width, int Height, string Format)>
            UploadAsync(IFormFile file, CancellationToken ct = default);

        Task DeleteAsync(string publicId, CancellationToken ct = default);
    }

}

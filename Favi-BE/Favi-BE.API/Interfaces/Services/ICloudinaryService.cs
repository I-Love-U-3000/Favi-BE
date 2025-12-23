using Favi_BE.Models.Dtos;

namespace Favi_BE.Interfaces.Services
{
    public interface ICloudinaryService
    {
        Task<PostMediaResponse?> TryUploadAsync(IFormFile file, CancellationToken ct = default);
        Task<PostMediaResponse> UploadAsyncOrThrow(IFormFile file, CancellationToken ct = default);
        Task<bool> TryDeleteAsync(string publicId, CancellationToken ct = default);
        Task<int> TryDeleteManyAsync(IEnumerable<string> publicIds, CancellationToken ct = default);
    }
}

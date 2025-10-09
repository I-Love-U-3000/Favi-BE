using Favi_BE.Models.Dtos;

namespace Favi_BE.Interfaces.Services
{
    // Interfaces/Services/ICloudinaryService.cs
    public interface ICloudinaryService
    {
        // Bản an toàn: không throw; null nếu upload fail
        Task<PostMediaResponse?> TryUploadAsync(IFormFile file, CancellationToken ct = default);

        // (Optional) Bản strict: throw nếu cần, giữ cho test nội bộ
        Task<PostMediaResponse> UploadAsyncOrThrow(IFormFile file, CancellationToken ct = default);
    }


}

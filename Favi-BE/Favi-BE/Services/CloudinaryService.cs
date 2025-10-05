using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Favi_BE.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Favi_BE.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            var section = config.GetSection("Cloudinary");
            var account = new Account(
                section["CloudName"],
                section["ApiKey"],
                section["ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        public async Task<(string Url, string? ThumbnailUrl, string PublicId, int Width, int Height, string Format)>
            UploadAsync(IFormFile file, CancellationToken ct = default)
        {
            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "favi_posts", // optional: để phân loại
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, ct);

            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("Cloudinary upload failed");

            return (
                Url: uploadResult.SecureUrl.ToString(),
                ThumbnailUrl: uploadResult?.Eager?.FirstOrDefault()?.SecureUrl?.ToString(),
                PublicId: uploadResult.PublicId,
                Width: uploadResult.Width, // Removed HasValue check as Width is an int, not nullable
                Height: uploadResult.Height, // Removed HasValue check as Height is an int, not nullable
                Format: uploadResult.Format
            );
        }

        public async Task DeleteAsync(string publicId, CancellationToken ct = default)
        {
            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image
            };

            // Fix: Use the correct overload of DestroyAsync that accepts only one argument
            await _cloudinary.DestroyAsync(deletionParams);
        }
    }
}

﻿using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
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

        public async Task<PostMediaResponse?> TryUploadAsync(IFormFile file, CancellationToken ct = default)
        {
            try
            {
                await using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "favi_posts",
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams, ct);
                if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                    return null; // ✅ bỏ throw

                // Trả về PostMediaResponse; Id/PostId/Position chưa có ở bước upload → set mặc định
                return new PostMediaResponse(
                    Id: Guid.Empty,
                    PostId: Guid.Empty,
                    Url: uploadResult.SecureUrl.ToString(),
                    PublicId: uploadResult.PublicId,
                    Width: uploadResult.Width,
                    Height: uploadResult.Height,
                    Format: uploadResult.Format,
                    Position: 0,
                    ThumbnailUrl: uploadResult?.Eager?.FirstOrDefault()?.SecureUrl?.ToString()
                );
            }
            catch
            {
                return null; // ✅ an toàn
            }
        }

        // (Optional) Bản strict nếu bạn vẫn muốn dùng ở chỗ khác
        public async Task<PostMediaResponse> UploadAsyncOrThrow(IFormFile file, CancellationToken ct = default)
        {
            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "favi_posts",
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, ct);
            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("Cloudinary upload failed"); // ⚠️ chỉ dùng khi bạn thật sự muốn throw

            return new PostMediaResponse(
                Id: Guid.Empty,
                PostId: Guid.Empty,
                Url: uploadResult.SecureUrl.ToString(),
                PublicId: uploadResult.PublicId,
                Width: uploadResult.Width,
                Height: uploadResult.Height,
                Format: uploadResult.Format,
                Position: 0,
                ThumbnailUrl: uploadResult?.Eager?.FirstOrDefault()?.SecureUrl?.ToString()
            );
        }
    }
}

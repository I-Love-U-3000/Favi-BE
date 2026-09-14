using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Favi_BE.API.Controllers;

[ApiController]
[Route("api/media")]
public class MediaCacheController : ControllerBase
{
    private readonly string _rootPath;
    private readonly ILogger<MediaCacheController> _logger;

    public MediaCacheController(
        IWebHostEnvironment env,
        IOptions<MediaCacheOptions> options,
        ILogger<MediaCacheController> logger)
    {
        _logger = logger;
        var relativePath = options.Value.LocalPath ?? "wwwroot/seed-assets";
        _rootPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, relativePath));
    }

    [HttpGet("cached/{category}/{fileName}")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)]
    public IActionResult GetCachedMedia(string category, string fileName)
    {
        // Sanitize to prevent path traversal
        var safeCategory = Path.GetFileName(category);
        var safeFileName = Path.GetFileName(fileName);
        var targetFile = Path.GetFullPath(Path.Combine(_rootPath, safeCategory, safeFileName));

        if (!targetFile.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[MEDIA_GATEWAY_DENIED] Attempted directory traversal: {Category}/{FileName}", category, fileName);
            return BadRequest("Invalid media path.");
        }

        if (!System.IO.File.Exists(targetFile))
        {
            _logger.LogDebug("[MEDIA_GATEWAY_MISS] Cached media file not found: {Path}", targetFile);
            return NotFound("Media asset not found in local cache.");
        }

        var ext = Path.GetExtension(targetFile).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };

        return PhysicalFile(targetFile, contentType, enableRangeProcessing: true);
    }
}

using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace Favi_BE.Services
{
    public class NSFWService : INSFWService
    {
        private readonly HttpClient _httpClient;
        private readonly NSFWOptions _options;
        private readonly ILogger<NSFWService> _logger;

        public NSFWService(
            HttpClient httpClient,
            IOptions<NSFWOptions> options,
            ILogger<NSFWService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public bool IsEnabled() => _options.Enabled;

        public async Task<bool> CheckPostAsync(Post post, CancellationToken ct = default)
        {
            if (!_options.Enabled)
            {
                _logger.LogDebug("NSFW detection is disabled. Skipping post {PostId}", post.Id);
                return false;
            }

            try
            {
                // Extract image URLs from PostMedias
                var imageUrls = post.PostMedias?
                    .OrderBy(m => m.Position)
                    .Select(m => m.Url)
                    .ToList() ?? new List<string>();

                // If no images and no caption, assume safe
                if (imageUrls.Count == 0 && string.IsNullOrWhiteSpace(post.Caption))
                {
                    return false;
                }

                // Create request
                var request = new NSFWCheckRequest(
                    ImageUrls: imageUrls,
                    Caption: post.Caption ?? "",
                    Threshold: _options.Threshold
                );

                _logger.LogInformation(
                    "Checking post {PostId} for NSFW content with {ImageCount} images",
                    post.Id, imageUrls.Count
                );

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

                var response = await _httpClient.PostAsJsonAsync(
                    "/nsfw/check",
                    request,
                    cts.Token
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<NSFWCheckResponse>(
                        cancellationToken: ct
                    );

                    if (result != null)
                    {
                        _logger.LogInformation(
                            "Post {PostId} NSFW check result: {IsNSFW}, Confidence: {Confidence}",
                            post.Id, result.IsNSFW, result.Confidence
                        );

                        return result.IsNSFW;
                    }
                }

                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Failed to check NSFW for post {PostId}. Status: {StatusCode}, Body: {ErrorBody}",
                    post.Id, response.StatusCode, errorBody
                );

                return false;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Timeout checking NSFW for post {PostId} after {TimeoutSeconds}s",
                    post.Id, _options.TimeoutSeconds);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking NSFW for post {PostId}", post.Id);
                return false;
            }
        }
    }
}

using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace Favi_BE.Services
{
    public class VectorIndexService : IVectorIndexService
    {
        private readonly HttpClient _httpClient;
        private readonly VectorIndexOptions _options;
        private readonly ILogger<VectorIndexService> _logger;

        public VectorIndexService(
            HttpClient httpClient,
            IOptions<VectorIndexOptions> options,
            ILogger<VectorIndexService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public bool IsEnabled() => _options.Enabled;

        public async Task<bool> IndexPostAsync(Post post, CancellationToken ct = default)
        {
            if (!_options.Enabled)
            {
                _logger.LogDebug("Vector indexing is disabled. Skipping post {PostId}", post.Id);
                return false;
            }

            try
            {
                // Map privacy enum to string expected by Python API
                var privacy = MapPrivacyLevel(post.Privacy);

                // Extract image URLs from PostMedias
                var imageUrls = post.PostMedias?
                    .OrderBy(m => m.Position)
                    .Select(m => m.Url)
                    .ToList() ?? new List<string>();

                // Create request matching Python API schema
                var request = new VectorIndexPostRequest(
                    PostId: post.Id.ToString(),
                    OwnerId: post.ProfileId.ToString(),
                    Privacy: privacy,
                    ImageUrls: imageUrls,
                    Caption: post.Caption ?? "",
                    Alpha: _options.Alpha
                );

                _logger.LogInformation(
                    "Indexing post {PostId} with {ImageCount} images and caption length {CaptionLength}",
                    post.Id, imageUrls.Count, post.Caption?.Length ?? 0
                );

                // POST to /posts endpoint
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

                var response = await _httpClient.PostAsJsonAsync(
                    "/posts",
                    request,
                    cts.Token
                );

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully indexed post {PostId}", post.Id);
                    return true;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning(
                        "Failed to index post {PostId}. Status: {StatusCode}, Body: {ErrorBody}",
                        post.Id, response.StatusCode, errorBody
                    );
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Timeout indexing post {PostId} after {TimeoutSeconds}s",
                    post.Id, _options.TimeoutSeconds);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing post {PostId}", post.Id);
                return false;
            }
        }

        public async Task<List<VectorSearchResultItem>> SearchAsync(
            Guid userId,
            string query,
            int k = 100,
            CancellationToken ct = default)
        {
            if (!_options.Enabled)
            {
                _logger.LogDebug("Vector search is disabled");
                return new List<VectorSearchResultItem>();
            }

            try
            {
                _logger.LogInformation(
                    "Semantic search for user {UserId}, query: '{Query}', k: {K}",
                    userId, query, k
                );

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

                // GET /search?user_id=xxx&q=xxx&k=xxx
                var url = $"/search?user_id={userId}&q={Uri.EscapeDataString(query)}&k={k}";
                var response = await _httpClient.GetAsync(url, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning(
                        "Semantic search failed. Status: {StatusCode}, Body: {ErrorBody}",
                        response.StatusCode, errorBody
                    );
                    return new List<VectorSearchResultItem>();
                }

                var result = await response.Content.ReadFromJsonAsync<List<VectorSearchResultItem>>(
                    cancellationToken: ct
                );

                _logger.LogInformation(
                    "Semantic search returned {Count} results for query '{Query}'",
                    result?.Count ?? 0, query
                );

                return result ?? new List<VectorSearchResultItem>();
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Timeout during semantic search after {TimeoutSeconds}s",
                    _options.TimeoutSeconds);
                return new List<VectorSearchResultItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during semantic search for query '{Query}'", query);
                return new List<VectorSearchResultItem>();
            }
        }

        /// <summary>
        /// Maps C# PrivacyLevel enum to string expected by Python API
        /// </summary>
        private string MapPrivacyLevel(PrivacyLevel privacy)
        {
            return privacy switch
            {
                PrivacyLevel.Public => "Public",
                PrivacyLevel.Followers => "Followers",
                PrivacyLevel.Private => "Private",
                _ => "Public"  // default fallback
            };
        }
    }
}

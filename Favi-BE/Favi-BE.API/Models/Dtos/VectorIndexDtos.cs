using System.Text.Json.Serialization;

namespace Favi_BE.Models.Dtos
{
    // Configuration options
    public class VectorIndexOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 10;
        public double Alpha { get; set; } = 0.5;
    }

    // DTO for indexing a post (matches Python API PostIn model)
    public record VectorIndexPostRequest(
        [property: JsonPropertyName("post_id")] string PostId,
        [property: JsonPropertyName("owner_id")] string OwnerId,
        [property: JsonPropertyName("privacy")] string Privacy,
        [property: JsonPropertyName("image_urls")] List<string> ImageUrls,
        [property: JsonPropertyName("caption")] string? Caption,
        [property: JsonPropertyName("alpha")] double Alpha
    );

    // Response from index operation
    public record VectorIndexPostResponse(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("post_id")] string PostId,
        [property: JsonPropertyName("image_count")] int ImageCount
    );

    // Individual search result from Vector API
    public record VectorSearchResultItem(
        [property: JsonPropertyName("post_id")] string PostId,
        [property: JsonPropertyName("owner_id")] string OwnerId,
        [property: JsonPropertyName("privacy")] string Privacy,
        [property: JsonPropertyName("image_urls")] List<string> ImageUrls,
        [property: JsonPropertyName("caption")] string? Caption,
        [property: JsonPropertyName("score")] double Score
    );

    // Response from Vector API search
    public record VectorSearchApiResponse(
        [property: JsonPropertyName("results")] List<VectorSearchResultItem>? Results
    );
}

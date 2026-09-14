using System.Text.Json.Serialization;

namespace Favi_BE.Models.Dtos
{
    // Configuration options
    public class VectorIndexOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 60;
        public double Alpha { get; set; } = 0.5;
        public int BulkBatchSize { get; set; } = 16;
        public int MaxConcurrency { get; set; } = 4;
    }

    // DTO for bulk post indexing
    public record BulkIndexPostsRequest(
        [property: JsonPropertyName("items")] List<VectorIndexPostRequest> Items,
        [property: JsonPropertyName("batch_size")] int BatchSize = 16
    );

    public record BulkIndexPostsResponse(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("inserted")] int Inserted,
        [property: JsonPropertyName("batch_size")] int BatchSize
    );

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

using System.Text.Json.Serialization;

namespace Favi_BE.Models.Dtos
{
    // Configuration options for NSFW detection service
    public class NSFWOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 30;
        public double Threshold { get; set; } = 0.7; // Confidence threshold for NSFW classification
    }

    // Request to check post content for NSFW
    public record NSFWCheckRequest(
        [property: JsonPropertyName("image_urls")] List<string> ImageUrls,
        [property: JsonPropertyName("caption")] string? Caption,
        [property: JsonPropertyName("threshold")] double Threshold
    );

    // Response from NSFW check API
    public record NSFWCheckResponse(
        [property: JsonPropertyName("is_nsfw")] bool IsNSFW,
        [property: JsonPropertyName("confidence")] double Confidence,
        [property: JsonPropertyName("categories")] Dictionary<string, double>? Categories,
        [property: JsonPropertyName("image_results")] List<NSFWImageResult>? ImageResults
    );

    // Individual image result
    public record NSFWImageResult(
        [property: JsonPropertyName("image_url")] string ImageUrl,
        [property: JsonPropertyName("is_nsfw")] bool IsNSFW,
        [property: JsonPropertyName("confidence")] double Confidence
    );
}

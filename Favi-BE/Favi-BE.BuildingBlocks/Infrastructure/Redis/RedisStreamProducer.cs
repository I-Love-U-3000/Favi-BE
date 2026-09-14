using System.Text.Json;
using Favi_BE.BuildingBlocks.Application.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Favi_BE.BuildingBlocks.Infrastructure.Redis;

public sealed class RedisStreamProducer : IRedisStreamProducer
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisStreamProducer> _logger;
    private readonly string _streamKey;

    public RedisStreamProducer(
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<RedisStreamProducer> logger)
    {
        _redis = redis;
        _logger = logger;
        _streamKey = configuration["Redis:StreamKey"] ?? "favi:stream:post-ai-tasks";
    }

    public async Task<string?> PublishPostAITaskAsync(
        Guid postId,
        string? caption,
        IReadOnlyList<string> imageUrls,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var enqueuedAt = DateTime.UtcNow;

            var entries = new NameValueEntry[]
            {
                new("postId", postId.ToString()),
                new("caption", caption ?? string.Empty),
                new("imageUrls", JsonSerializer.Serialize(imageUrls)),
                new("enqueuedAt", enqueuedAt.ToString("o"))
            };

            var messageId = await db.StreamAddAsync(_streamKey, entries);

            _logger.LogInformation(
                "[AI_QUEUE_ENQUEUED] Post {PostId} enqueued to Redis Stream {StreamKey} (MessageId: {MessageId}) at {EnqueuedAt}",
                postId, _streamKey, messageId, enqueuedAt);

            return messageId.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[AI_QUEUE_FAILED] Failed to enqueue Post {PostId} to Redis Stream {StreamKey}",
                postId, _streamKey);

            return null;
        }
    }
}

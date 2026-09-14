namespace Favi_BE.Models.Dtos;

public sealed class MediaCacheOptions
{
    public string LocalPath { get; set; } = "wwwroot/seed-assets";
    public int MaxCacheSizeMB { get; set; } = 5000;
}

public sealed class RedisStreamOptions
{
    public string ConnectionString { get; set; } = "redis:6379";
    public string StreamKey { get; set; } = "favi:stream:post-ai-tasks";
    public string ConsumerGroup { get; set; } = "ai_processors";
    public string ConsumerName { get; set; } = "favi-worker-1";
}

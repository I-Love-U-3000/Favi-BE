namespace Favi_BE.BuildingBlocks.Application.Redis;

public interface IRedisStreamProducer
{
    Task<string?> PublishPostAITaskAsync(
        Guid postId,
        string? caption,
        IReadOnlyList<string> imageUrls,
        CancellationToken cancellationToken = default);
}

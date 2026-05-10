namespace Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;

public enum FeedItemKind { Post, Repost }

public sealed record FeedItemReadModel(
    FeedItemKind Kind,
    PostReadModel? Post,
    RepostReadModel? Repost,
    DateTime CreatedAt);

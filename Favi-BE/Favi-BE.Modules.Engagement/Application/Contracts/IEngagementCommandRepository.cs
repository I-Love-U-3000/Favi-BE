using Favi_BE.Modules.Engagement.Application.Contracts.WriteModels;

namespace Favi_BE.Modules.Engagement.Application.Contracts;

public interface IEngagementCommandRepository
{
    // Comment write
    Task AddCommentAsync(CommentWriteData comment, CancellationToken ct = default);
    Task<CommentWriteData?> GetCommentByIdAsync(Guid commentId, CancellationToken ct = default);
    Task UpdateCommentAsync(CommentWriteData comment, CancellationToken ct = default);
    Task RemoveCommentAsync(Guid commentId, CancellationToken ct = default);

    // Reaction write
    Task<ReactionWriteData?> GetPostReactionByActorAsync(Guid actorId, Guid postId, CancellationToken ct = default);
    Task<ReactionWriteData?> GetCommentReactionByActorAsync(Guid actorId, Guid commentId, CancellationToken ct = default);
    Task<ReactionWriteData?> GetCollectionReactionByActorAsync(Guid actorId, Guid collectionId, CancellationToken ct = default);
    Task<ReactionWriteData?> GetRepostReactionByActorAsync(Guid actorId, Guid repostId, CancellationToken ct = default);
    Task AddReactionAsync(ReactionWriteData reaction, CancellationToken ct = default);
    Task UpdateReactionTypeAsync(Guid reactionId, Domain.ReactionType newType, CancellationToken ct = default);
    Task RemoveReactionAsync(Guid reactionId, CancellationToken ct = default);

    // Cross-context read lookups (for business rules + notification routing)
    Task<bool> PostExistsAsync(Guid postId, CancellationToken ct = default);
    Task<bool> CollectionExistsAsync(Guid collectionId, CancellationToken ct = default);
    Task<bool> RepostExistsAsync(Guid repostId, CancellationToken ct = default);
    Task<Guid?> GetPostAuthorIdAsync(Guid postId, CancellationToken ct = default);
    Task<Guid?> GetCommentAuthorIdAsync(Guid commentId, CancellationToken ct = default);
    Task<Guid?> GetCollectionAuthorIdAsync(Guid collectionId, CancellationToken ct = default);
    Task<ActorProfileData?> GetActorProfileAsync(Guid profileId, CancellationToken ct = default);

    Task SaveAsync(CancellationToken ct = default);
}

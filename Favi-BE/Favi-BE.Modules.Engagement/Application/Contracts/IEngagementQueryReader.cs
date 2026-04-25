using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Engagement.Application.Contracts;

public interface IEngagementQueryReader
{
    Task<(IReadOnlyList<CommentQueryDto> Items, int TotalCount)> GetCommentsByPostAsync(
        Guid postId, Guid? currentUserId, int page, int pageSize, CancellationToken ct = default);

    Task<CommentQueryDto?> GetCommentByIdAsync(
        Guid commentId, Guid? currentUserId, CancellationToken ct = default);

    Task<ReactionSummaryQueryDto> GetReactionSummaryForPostAsync(
        Guid postId, Guid? currentUserId, CancellationToken ct = default);

    Task<ReactionSummaryQueryDto> GetReactionSummaryForCommentAsync(
        Guid commentId, Guid? currentUserId, CancellationToken ct = default);

    Task<ReactionSummaryQueryDto> GetReactionSummaryForCollectionAsync(
        Guid collectionId, Guid? currentUserId, CancellationToken ct = default);

    Task<IReadOnlyList<ReactorQueryDto>> GetReactorsForPostAsync(
        Guid postId, CancellationToken ct = default);

    Task<IReadOnlyList<ReactorQueryDto>> GetReactorsForCommentAsync(
        Guid commentId, CancellationToken ct = default);

    Task<IReadOnlyList<ReactorQueryDto>> GetReactorsForCollectionAsync(
        Guid collectionId, CancellationToken ct = default);
}

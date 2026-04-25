using Favi_BE.Data;
using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using Favi_BE.Modules.Engagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Application.Engagement;

/// <summary>
/// Implements IEngagementQueryReader using AppDbContext with AsNoTracking projections.
/// No mutations are performed here — pure read side.
/// </summary>
internal sealed class EngagementQueryReaderAdapter : IEngagementQueryReader
{
    private readonly AppDbContext _db;

    public EngagementQueryReaderAdapter(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<CommentQueryDto> Items, int TotalCount)> GetCommentsByPostAsync(
        Guid postId, Guid? currentUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;

        var total = await _db.Comments
            .AsNoTracking()
            .CountAsync(c => c.PostId == postId, ct);

        var comments = await _db.Comments
            .AsNoTracking()
            .Where(c => c.PostId == postId)
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(c => new { c.Id, c.PostId, c.RepostId, c.ProfileId, c.Content, c.MediaUrl, c.ParentCommentId, c.CreatedAt, c.UpdatedAt })
            .ToListAsync(ct);

        var commentIds = comments.Select(c => c.Id).ToList();

        var reactions = await _db.Reactions
            .AsNoTracking()
            .Where(r => r.CommentId != null && commentIds.Contains(r.CommentId.Value))
            .Select(r => new { r.CommentId, r.ProfileId, r.Type })
            .ToListAsync(ct);

        var reactionsByComment = reactions
            .GroupBy(r => r.CommentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var dtos = comments.Select(c =>
        {
            var commentReactions = reactionsByComment.TryGetValue(c.Id, out var list) ? list : [];

            var byType = commentReactions
                .GroupBy(r => r.Type)
                .ToDictionary(g => (ReactionType)(int)g.Key, g => g.Count());

            ReactionType? mine = null;
            if (currentUserId.HasValue)
            {
                var my = commentReactions.FirstOrDefault(r => r.ProfileId == currentUserId.Value);
                if (my != null) mine = (ReactionType)(int)my.Type;
            }

            var summary = new ReactionSummaryQueryDto(byType.Values.Sum(), byType, mine);

            return new CommentQueryDto(
                c.Id, c.PostId, c.RepostId, c.ProfileId,
                c.Content, c.MediaUrl, c.ParentCommentId,
                c.CreatedAt, c.UpdatedAt, summary);
        }).ToList();

        return (dtos, total);
    }

    public async Task<CommentQueryDto?> GetCommentByIdAsync(
        Guid commentId, Guid? currentUserId, CancellationToken ct = default)
    {
        var c = await _db.Comments
            .AsNoTracking()
            .Where(x => x.Id == commentId)
            .Select(x => new { x.Id, x.PostId, x.RepostId, x.ProfileId, x.Content, x.MediaUrl, x.ParentCommentId, x.CreatedAt, x.UpdatedAt })
            .FirstOrDefaultAsync(ct);

        if (c is null) return null;

        var summary = await GetReactionSummaryForCommentAsync(commentId, currentUserId, ct);

        return new CommentQueryDto(
            c.Id, c.PostId, c.RepostId, c.ProfileId,
            c.Content, c.MediaUrl, c.ParentCommentId,
            c.CreatedAt, c.UpdatedAt, summary);
    }

    public async Task<ReactionSummaryQueryDto> GetReactionSummaryForPostAsync(
        Guid postId, Guid? currentUserId, CancellationToken ct = default)
    {
        var reactions = await _db.Reactions
            .AsNoTracking()
            .Where(r => r.PostId == postId)
            .Select(r => new { r.ProfileId, r.Type })
            .ToListAsync(ct);

        return BuildSummary(reactions.Select(r => (r.ProfileId, r.Type)), currentUserId);
    }

    public async Task<ReactionSummaryQueryDto> GetReactionSummaryForCommentAsync(
        Guid commentId, Guid? currentUserId, CancellationToken ct = default)
    {
        var reactions = await _db.Reactions
            .AsNoTracking()
            .Where(r => r.CommentId == commentId)
            .Select(r => new { r.ProfileId, r.Type })
            .ToListAsync(ct);

        return BuildSummary(reactions.Select(r => (r.ProfileId, r.Type)), currentUserId);
    }

    public async Task<ReactionSummaryQueryDto> GetReactionSummaryForCollectionAsync(
        Guid collectionId, Guid? currentUserId, CancellationToken ct = default)
    {
        var reactions = await _db.Reactions
            .AsNoTracking()
            .Where(r => r.CollectionId == collectionId)
            .Select(r => new { r.ProfileId, r.Type })
            .ToListAsync(ct);

        return BuildSummary(reactions.Select(r => (r.ProfileId, r.Type)), currentUserId);
    }

    public async Task<IReadOnlyList<ReactorQueryDto>> GetReactorsForPostAsync(
        Guid postId, CancellationToken ct = default)
    {
        var results = await _db.Reactions
            .AsNoTracking()
            .Where(r => r.PostId == postId)
            .Select(r => new ReactorQueryDto(
                r.Profile.Id,
                r.Profile.Username,
                r.Profile.DisplayName,
                r.Profile.AvatarUrl,
                (ReactionType)(int)r.Type,
                r.CreatedAt))
            .ToListAsync(ct);

        return results;
    }

    public async Task<IReadOnlyList<ReactorQueryDto>> GetReactorsForCommentAsync(
        Guid commentId, CancellationToken ct = default)
    {
        var results = await _db.Reactions
            .AsNoTracking()
            .Where(r => r.CommentId == commentId)
            .Select(r => new ReactorQueryDto(
                r.Profile.Id,
                r.Profile.Username,
                r.Profile.DisplayName,
                r.Profile.AvatarUrl,
                (ReactionType)(int)r.Type,
                r.CreatedAt))
            .ToListAsync(ct);

        return results;
    }

    public async Task<IReadOnlyList<ReactorQueryDto>> GetReactorsForCollectionAsync(
        Guid collectionId, CancellationToken ct = default)
    {
        var results = await _db.Reactions
            .AsNoTracking()
            .Where(r => r.CollectionId == collectionId)
            .Select(r => new ReactorQueryDto(
                r.Profile.Id,
                r.Profile.Username,
                r.Profile.DisplayName,
                r.Profile.AvatarUrl,
                (ReactionType)(int)r.Type,
                r.CreatedAt))
            .ToListAsync(ct);

        return results;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static ReactionSummaryQueryDto BuildSummary(
        IEnumerable<(Guid ProfileId, Favi_BE.Models.Enums.ReactionType Type)> reactions,
        Guid? currentUserId)
    {
        var list = reactions.ToList();

        var byType = list
            .GroupBy(r => r.Type)
            .ToDictionary(g => (ReactionType)(int)g.Key, g => g.Count());

        ReactionType? mine = null;
        if (currentUserId.HasValue)
        {
            var my = list.FirstOrDefault(r => r.ProfileId == currentUserId.Value);
            if (my != default) mine = (ReactionType)(int)my.Type;
        }

        return new ReactionSummaryQueryDto(byType.Values.Sum(), byType, mine);
    }
}

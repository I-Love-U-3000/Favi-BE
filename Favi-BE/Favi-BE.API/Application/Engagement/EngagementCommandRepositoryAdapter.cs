using Favi_BE.Data;
using Favi_BE.Interfaces;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Modules.Engagement.Application.Contracts;
using Favi_BE.Modules.Engagement.Application.Contracts.WriteModels;
using Microsoft.EntityFrameworkCore;
using EngagementReactionType = Favi_BE.Modules.Engagement.Domain.ReactionType;
using LegacyReactionType = Favi_BE.Models.Enums.ReactionType;

namespace Favi_BE.API.Application.Engagement;

/// <summary>
/// Implements IEngagementCommandRepository using the existing IUnitOfWork infrastructure.
/// Keeps the Engagement module free of API-layer dependencies.
/// </summary>
internal sealed class EngagementCommandRepositoryAdapter : IEngagementCommandRepository
{
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;

    public EngagementCommandRepositoryAdapter(IUnitOfWork uow, AppDbContext db)
    {
        _uow = uow;
        _db = db;
    }

    // -------------------------------------------------------------------------
    // Comment write
    // -------------------------------------------------------------------------

    public async Task AddCommentAsync(CommentWriteData comment, CancellationToken ct = default)
    {
        await _uow.Comments.AddAsync(new Comment
        {
            Id = comment.Id,
            PostId = comment.PostId,
            RepostId = comment.RepostId,
            ProfileId = comment.ProfileId,
            Content = comment.Content,
            MediaUrl = comment.MediaUrl,
            ParentCommentId = comment.ParentCommentId,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        });
    }

    public async Task<CommentWriteData?> GetCommentByIdAsync(Guid commentId, CancellationToken ct = default)
    {
        var c = await _uow.Comments.GetByIdAsync(commentId);
        return c is null ? null : MapComment(c);
    }

    public async Task UpdateCommentAsync(CommentWriteData comment, CancellationToken ct = default)
    {
        var entity = await _uow.Comments.GetByIdAsync(comment.Id);
        if (entity is null) return;

        entity.Content = comment.Content;
        entity.UpdatedAt = comment.UpdatedAt;
        _uow.Comments.Update(entity);
    }

    public async Task RemoveCommentAsync(Guid commentId, CancellationToken ct = default)
    {
        var entity = await _uow.Comments.GetByIdAsync(commentId);
        if (entity is null) return;
        _uow.Comments.Remove(entity);
    }

    // -------------------------------------------------------------------------
    // Reaction write
    // -------------------------------------------------------------------------

    public async Task<ReactionWriteData?> GetPostReactionByActorAsync(Guid actorId, Guid postId, CancellationToken ct = default)
    {
        var r = await _uow.Reactions.GetProfileReactionOnPostAsync(actorId, postId);
        return r is null ? null : MapReaction(r);
    }

    public async Task<ReactionWriteData?> GetCommentReactionByActorAsync(Guid actorId, Guid commentId, CancellationToken ct = default)
    {
        var r = await _uow.Reactions.GetProfileReactionOnCommentAysnc(actorId, commentId);
        return r is null ? null : MapReaction(r);
    }

    public async Task<ReactionWriteData?> GetCollectionReactionByActorAsync(Guid actorId, Guid collectionId, CancellationToken ct = default)
    {
        var r = await _uow.Reactions.GetProfileReactionOnCollectionAsync(actorId, collectionId);
        return r is null ? null : MapReaction(r);
    }

    public async Task<ReactionWriteData?> GetRepostReactionByActorAsync(Guid actorId, Guid repostId, CancellationToken ct = default)
    {
        var r = await _db.Reactions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProfileId == actorId && x.RepostId == repostId, ct);
        return r is null ? null : MapReaction(r);
    }

    public async Task AddReactionAsync(ReactionWriteData reaction, CancellationToken ct = default)
    {
        await _uow.Reactions.AddAsync(new Reaction
        {
            Id = reaction.Id,
            ProfileId = reaction.ProfileId,
            PostId = reaction.PostId,
            CommentId = reaction.CommentId,
            CollectionId = reaction.CollectionId,
            RepostId = reaction.RepostId,
            Type = MapReactionType(reaction.Type),
            CreatedAt = reaction.CreatedAt
        });
    }

    public async Task UpdateReactionTypeAsync(Guid reactionId, EngagementReactionType newType, CancellationToken ct = default)
    {
        var entity = await _db.Reactions.FindAsync([reactionId], ct);
        if (entity is null) return;
        entity.Type = MapReactionType(newType);
        _uow.Reactions.Update(entity);
    }

    public async Task RemoveReactionAsync(Guid reactionId, CancellationToken ct = default)
    {
        var entity = await _db.Reactions.FindAsync([reactionId], ct);
        if (entity is null) return;
        _uow.Reactions.Remove(entity);
    }

    // -------------------------------------------------------------------------
    // Cross-context read lookups
    // -------------------------------------------------------------------------

    public async Task<bool> PostExistsAsync(Guid postId, CancellationToken ct = default)
        => await _db.Posts.AnyAsync(p => p.Id == postId, ct);

    public async Task<bool> CollectionExistsAsync(Guid collectionId, CancellationToken ct = default)
        => await _db.Collections.AnyAsync(c => c.Id == collectionId, ct);

    public async Task<bool> RepostExistsAsync(Guid repostId, CancellationToken ct = default)
        => await _db.Reposts.AnyAsync(r => r.Id == repostId, ct);

    public async Task<Guid?> GetPostAuthorIdAsync(Guid postId, CancellationToken ct = default)
    {
        var post = await _uow.Posts.GetByIdAsync(postId);
        return post?.ProfileId;
    }

    public async Task<Guid?> GetCommentAuthorIdAsync(Guid commentId, CancellationToken ct = default)
    {
        var comment = await _uow.Comments.GetByIdAsync(commentId);
        return comment?.ProfileId;
    }

    public async Task<Guid?> GetCollectionAuthorIdAsync(Guid collectionId, CancellationToken ct = default)
    {
        var collection = await _uow.Collections.GetByIdAsync(collectionId);
        return collection?.ProfileId;
    }

    public async Task<ActorProfileData?> GetActorProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        var profile = await _uow.Profiles.GetByIdAsync(profileId);
        return profile is null
            ? null
            : new ActorProfileData(profile.Id, profile.Username, profile.DisplayName, profile.AvatarUrl);
    }

    public async Task SaveAsync(CancellationToken ct = default)
        => await _uow.CompleteAsync();

    // -------------------------------------------------------------------------
    // Mapping helpers
    // -------------------------------------------------------------------------

    private static CommentWriteData MapComment(Comment c) =>
        new(c.Id, c.PostId, c.RepostId, c.ProfileId, c.Content, c.MediaUrl,
            c.ParentCommentId, c.CreatedAt, c.UpdatedAt);

    private static ReactionWriteData MapReaction(Reaction r) =>
        new(r.Id, r.ProfileId, r.PostId, r.CommentId, r.CollectionId, r.RepostId,
            MapReactionType(r.Type), r.CreatedAt);

    private static EngagementReactionType MapReactionType(LegacyReactionType t) =>
        (EngagementReactionType)(int)t;

    private static LegacyReactionType MapReactionType(EngagementReactionType t) =>
        (LegacyReactionType)(int)t;
}

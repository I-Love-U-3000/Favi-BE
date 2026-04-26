using Favi_BE.API.Models.Entities;
using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Entities.JoinTables;
using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels;
using Favi_BE.Modules.ContentPublishing.Domain;
using LegacyPrivacy = Favi_BE.Models.Enums.PrivacyLevel;

namespace Favi_BE.API.Application.ContentPublishing;

internal sealed class ContentPublishingCommandRepositoryAdapter : IContentPublishingCommandRepository
{
    private readonly IUnitOfWork _uow;
    private readonly IPrivacyGuard _privacy;

    public ContentPublishingCommandRepositoryAdapter(IUnitOfWork uow, IPrivacyGuard privacy)
    {
        _uow = uow;
        _privacy = privacy;
    }

    // ── Post ──────────────────────────────────────────────────────────────

    public async Task AddPostAsync(PostWriteData data, CancellationToken ct = default)
        => await _uow.Posts.AddAsync(new Post
        {
            Id = data.Id,
            ProfileId = data.ProfileId,
            Caption = data.Caption,
            Privacy = MapPrivacy(data.Privacy),
            LocationName = data.LocationName,
            LocationFullAddress = data.LocationFullAddress,
            LocationLatitude = data.LocationLatitude,
            LocationLongitude = data.LocationLongitude,
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt,
            IsArchived = data.IsArchived,
            DeletedDayExpiredAt = data.DeletedDayExpiredAt
        });

    public async Task<PostWriteData?> GetPostForWriteAsync(Guid postId, CancellationToken ct = default)
    {
        var post = await _uow.Posts.GetByIdAsync(postId);
        return post is null ? null : MapPost(post);
    }

    public async Task UpdatePostAsync(PostWriteData data, CancellationToken ct = default)
    {
        var post = await _uow.Posts.GetByIdAsync(data.Id);
        if (post is null) return;

        post.Caption = data.Caption;
        post.Privacy = MapPrivacy(data.Privacy);
        post.UpdatedAt = data.UpdatedAt;

        _uow.Posts.Update(post);
    }

    public async Task SoftDeletePostAsync(Guid postId, DateTime expiredAt, CancellationToken ct = default)
    {
        var post = await _uow.Posts.GetByIdAsync(postId);
        if (post is null) return;

        post.DeletedDayExpiredAt = expiredAt;
        post.UpdatedAt = DateTime.UtcNow;
        _uow.Posts.Update(post);
    }

    public async Task SetPostArchivedAsync(Guid postId, bool isArchived, CancellationToken ct = default)
    {
        var post = await _uow.Posts.GetByIdAsync(postId);
        if (post is null) return;

        post.IsArchived = isArchived;
        post.UpdatedAt = DateTime.UtcNow;
        _uow.Posts.Update(post);
    }

    // ── PostMedia ─────────────────────────────────────────────────────────

    public async Task AddPostMediaRangeAsync(IEnumerable<PostMediaWriteData> items, CancellationToken ct = default)
    {
        var entities = items.Select(m => new PostMedia
        {
            Id = m.Id,
            PostId = m.PostId,
            Url = m.Url,
            ThumbnailUrl = m.ThumbnailUrl,
            PublicId = m.PublicId,
            Width = m.Width,
            Height = m.Height,
            Format = m.Format,
            Position = m.Position
        }).ToList();

        await _uow.PostMedia.AddRangeAsync(entities);
    }

    public async Task<IReadOnlyList<PostMediaWriteData>> GetPostMediaAsync(Guid postId, CancellationToken ct = default)
    {
        var items = await _uow.PostMedia.GetByPostIdAsync(postId);
        return items.Select(m => new PostMediaWriteData(
            m.Id, m.PostId ?? Guid.Empty, m.Url, m.ThumbnailUrl,
            m.PublicId, m.Width, m.Height, m.Format, m.Position
        )).ToList();
    }

    public async Task UpdatePostMediaPositionsAsync(Guid postId, IEnumerable<(Guid MediaId, int Position)> positions, CancellationToken ct = default)
    {
        var existing = (await _uow.PostMedia.GetByPostIdAsync(postId)).ToList();
        var posMap = positions.ToDictionary(p => p.MediaId, p => p.Position);

        foreach (var media in existing)
        {
            if (posMap.TryGetValue(media.Id, out var newPos) && media.Position != newPos)
            {
                media.Position = newPos;
                _uow.PostMedia.Update(media);
            }
        }
    }

    // ── PostTags ──────────────────────────────────────────────────────────

    public async Task AddPostTagsAsync(Guid postId, IEnumerable<string> tagNames, CancellationToken ct = default)
    {
        var tags = await _uow.Tags.GetOrCreateTagsAsync(tagNames);
        foreach (var tag in tags)
            await _uow.PostTags.AddTagToPostAsync(postId, tag.Id);
    }

    public async Task RemovePostTagAsync(Guid postId, Guid tagId, CancellationToken ct = default)
        => await _uow.PostTags.RemoveTagFromPostAsync(postId, tagId);

    // ── Collection ────────────────────────────────────────────────────────

    public async Task AddCollectionAsync(CollectionWriteData data, CancellationToken ct = default)
        => await _uow.Collections.AddAsync(new Collection
        {
            Id = data.Id,
            ProfileId = data.ProfileId,
            Title = data.Title,
            Description = data.Description,
            CoverImageUrl = data.CoverImageUrl,
            CoverImagePublicId = data.CoverImagePublicId,
            PrivacyLevel = MapCollectionPrivacy(data.Privacy),
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt
        });

    public async Task<CollectionWriteData?> GetCollectionForWriteAsync(Guid collectionId, CancellationToken ct = default)
    {
        var c = await _uow.Collections.GetByIdAsync(collectionId);
        return c is null ? null : new CollectionWriteData(
            c.Id, c.ProfileId, c.Title, c.Description,
            c.CoverImageUrl, c.CoverImagePublicId,
            MapCollectionPrivacy(c.PrivacyLevel),
            c.CreatedAt, c.UpdatedAt
        );
    }

    public async Task UpdateCollectionAsync(CollectionWriteData data, CancellationToken ct = default)
    {
        var c = await _uow.Collections.GetByIdAsync(data.Id);
        if (c is null) return;

        c.Title = data.Title;
        c.Description = data.Description;
        c.CoverImageUrl = data.CoverImageUrl;
        c.CoverImagePublicId = data.CoverImagePublicId;
        c.PrivacyLevel = MapCollectionPrivacy(data.Privacy);
        c.UpdatedAt = data.UpdatedAt;

        _uow.Collections.Update(c);
    }

    public async Task DeleteCollectionAsync(Guid collectionId, CancellationToken ct = default)
    {
        var c = await _uow.Collections.GetByIdAsync(collectionId);
        if (c is not null)
            _uow.Collections.Remove(c);
    }

    public async Task<bool> IsPostInCollectionAsync(Guid collectionId, Guid postId, CancellationToken ct = default)
        => await _uow.PostCollections.ExistsInCollectionAsync(postId, collectionId);

    public async Task AddPostToCollectionAsync(Guid collectionId, Guid postId, CancellationToken ct = default)
        => await _uow.PostCollections.AddAsync(new PostCollection
        {
            CollectionId = collectionId,
            PostId = postId
        });

    public async Task RemovePostFromCollectionAsync(Guid collectionId, Guid postId, CancellationToken ct = default)
        => await _uow.PostCollections.RemoveFromCollectionAsync(postId, collectionId);

    // ── Repost ────────────────────────────────────────────────────────────

    public async Task AddRepostAsync(RepostWriteData data, CancellationToken ct = default)
        => await _uow.Reposts.AddAsync(new Repost
        {
            Id = data.Id,
            ProfileId = data.ProfileId,
            OriginalPostId = data.OriginalPostId,
            Caption = data.Caption,
            CreatedAt = data.CreatedAt,
            UpdatedAt = data.UpdatedAt
        });

    public async Task<RepostWriteData?> GetRepostAsync(Guid sharerId, Guid originalPostId, CancellationToken ct = default)
    {
        var r = await _uow.Reposts.GetRepostAsync(sharerId, originalPostId);
        return r is null ? null : new RepostWriteData(r.Id, r.ProfileId, r.OriginalPostId, r.Caption, r.CreatedAt, r.UpdatedAt);
    }

    public async Task RemoveRepostAsync(Guid sharerId, Guid originalPostId, CancellationToken ct = default)
    {
        var r = await _uow.Reposts.GetRepostAsync(sharerId, originalPostId);
        if (r is not null)
            _uow.Reposts.Remove(r);
    }

    // ── Cross-context checks ──────────────────────────────────────────────

    public async Task<bool> PostExistsAsync(Guid postId, CancellationToken ct = default)
        => await _uow.Posts.GetByIdAsync(postId) is not null;

    public async Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default)
        => await _uow.Profiles.GetByIdAsync(profileId) is not null;

    public async Task<bool> CanProfileViewPostAsync(Guid? viewerId, Guid postId, CancellationToken ct = default)
    {
        var post = await _uow.Posts.GetByIdAsync(postId);
        if (post is null) return false;
        return await _privacy.CanViewPostAsync(post, viewerId);
    }

    public async Task SaveAsync(CancellationToken ct = default)
        => await _uow.CompleteAsync();

    // ── Private mappers ───────────────────────────────────────────────────

    private static LegacyPrivacy MapPrivacy(PostPrivacy p) => (LegacyPrivacy)(int)p;

    private static LegacyPrivacy MapCollectionPrivacy(CollectionPrivacy p) => (LegacyPrivacy)(int)p;

    private static CollectionPrivacy MapCollectionPrivacy(LegacyPrivacy p) => (CollectionPrivacy)(int)p;

    private static PostWriteData MapPost(Post p) => new(
        p.Id, p.ProfileId, p.Caption, (PostPrivacy)(int)p.Privacy,
        p.LocationName, p.LocationFullAddress, p.LocationLatitude, p.LocationLongitude,
        p.CreatedAt, p.UpdatedAt, p.IsArchived, p.DeletedDayExpiredAt
    );
}

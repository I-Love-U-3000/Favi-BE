using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetArchivedPosts;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetExploreFeed;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetFeedWithReposts;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetGuestFeed;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetLatestFeed;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetNewsFeed;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetPostById;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetProfilePosts;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetRecycleBin;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetRepostById;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetRepostsByProfile;
using Favi_BE.Modules.ContentPublishing.Application.Commands.AddPostMedia;
using Favi_BE.Modules.ContentPublishing.Application.Commands.ArchivePost;
using Favi_BE.Modules.ContentPublishing.Application.Commands.CreatePost;
using Favi_BE.Modules.ContentPublishing.Application.Commands.DeletePost;
using Favi_BE.Modules.ContentPublishing.Application.Commands.PermanentDeletePost;
using Favi_BE.Modules.ContentPublishing.Application.Commands.RestorePost;
using Favi_BE.Modules.ContentPublishing.Application.Commands.SharePost;
using Favi_BE.Modules.ContentPublishing.Application.Commands.UnsharePost;
using Favi_BE.Modules.ContentPublishing.Application.Commands.UpdatePost;
using Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels;
using Favi_BE.Modules.ContentPublishing.Domain;
using Favi_BE.Modules.Engagement.Application.Commands.TogglePostReaction;
using Favi_BE.Modules.Engagement.Application.Queries.GetPostReactions;
using Favi_BE.Modules.Engagement.Application.Queries.GetPostReactors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EngagementReactionType = Favi_BE.Modules.Engagement.Domain.ReactionType;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly ITagService _tags;
        private readonly ISearchService _search;
        private readonly ICloudinaryService _cloudinary;
        private readonly IMediator _mediator;

        public PostsController(
            ITagService tags,
            ISearchService search,
            ICloudinaryService cloudinary,
            IMediator mediator)
        {
            _tags = tags;
            _search = search;
            _cloudinary = cloudinary;
            _mediator = mediator;
        }

        private Guid? TryGetUserId()
        {
            if (User?.Identity?.IsAuthenticated != true) return null;
            try { return User.GetUserId(); }
            catch { return null; }
        }

        // ======================
        // GET: Chi tiết post
        // ======================
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PostResponse>> GetById(Guid id)
        {
            var viewerId = TryGetUserId();
            var post = await _mediator.Send(new GetPostByIdQuery(id, viewerId));
            if (post is null)
                return NotFound(new { code = "POST_NOT_FOUND", message = "Bài viết không tồn tại hoặc đã bị xoá." });

            var reactions = await _mediator.Send(new GetPostReactionsQuery(id, viewerId));
            return Ok(MapToPostResponse(post, reactions));
        }

        // ======================
        // GET: Bài viết theo Profile
        // ======================
        [HttpGet("profile/{profileId:guid}")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetByProfile(
            Guid profileId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var viewerId = TryGetUserId();
            try
            {
                var (items, total) = await _mediator.Send(new GetProfilePostsQuery(profileId, viewerId, page, pageSize));
                var responses = new List<PostResponse>();
                foreach (var p in items)
                {
                    var reactions = await _mediator.Send(new GetPostReactionsQuery(p.Id, viewerId));
                    responses.Add(MapToPostResponse(p, reactions));
                }
                return Ok(new PagedResult<PostResponse>(responses, page, pageSize, total));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Hồ sơ không tồn tại." });
            }
        }

        // ======================
        // GET: Feed cá nhân
        // ======================
        [Authorize]
        [HttpGet("feed")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetPersonalFeed(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.GetUserId();
            var (items, total) = await _mediator.Send(new GetNewsFeedQuery(userId, page, pageSize));
            var responses = new List<PostResponse>();
            foreach (var p in items)
            {
                var reactions = await _mediator.Send(new GetPostReactionsQuery(p.Id, userId));
                responses.Add(MapToPostResponse(p, reactions));
            }
            return Ok(new PagedResult<PostResponse>(responses, page, pageSize, total));
        }

        // ======================
        // GET: Feed với Reposts
        // ======================
        [Authorize]
        [HttpGet("feed-with-reposts")]
        public async Task<ActionResult<PagedResult<FeedItemDto>>> GetFeedWithReposts(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.GetUserId();
            var (items, total) = await _mediator.Send(new GetFeedWithRepostsQuery(userId, page, pageSize));

            var dtos = new List<FeedItemDto>();
            foreach (var item in items)
            {
                if (item.Kind == FeedItemKind.Post && item.Post is not null)
                {
                    var reactions = await _mediator.Send(new GetPostReactionsQuery(item.Post.Id, userId));
                    dtos.Add(new FeedItemDto(FeedItemType.Post, MapToPostResponse(item.Post, reactions), null, item.CreatedAt));
                }
                else if (item.Kind == FeedItemKind.Repost && item.Repost is not null)
                {
                    var repostReactions = await _mediator.Send(new GetPostReactionsQuery(item.Repost.OriginalPostId, userId));
                    dtos.Add(new FeedItemDto(FeedItemType.Repost, null, MapToRepostResponse(item.Repost, repostReactions), item.CreatedAt));
                }
            }

            return Ok(new PagedResult<FeedItemDto>(dtos, page, pageSize, total));
        }

        // ======================
        // GET: Feed cho Guest
        // ======================
        [HttpGet("guest-feed")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetGuestFeed(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var (items, total) = await _mediator.Send(new GetGuestFeedQuery(page, pageSize));
            var responses = new List<PostResponse>();
            foreach (var p in items)
            {
                var reactions = await _mediator.Send(new GetPostReactionsQuery(p.Id, null));
                responses.Add(MapToPostResponse(p, reactions));
            }
            return Ok(new PagedResult<PostResponse>(responses, page, pageSize, total));
        }

        // ======================
        // GET: Explore
        // ======================
        [Authorize]
        [HttpGet("explore")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetExplore(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.GetUserId();
            var (items, total) = await _mediator.Send(new GetExploreFeedQuery(userId, page, pageSize));
            var responses = new List<PostResponse>();
            foreach (var p in items)
            {
                var reactions = await _mediator.Send(new GetPostReactionsQuery(p.Id, userId));
                responses.Add(MapToPostResponse(p, reactions));
            }
            return Ok(new PagedResult<PostResponse>(responses, page, pageSize, total));
        }

        // ======================
        // GET: Latest
        // ======================
        [HttpGet("latest")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetLatest(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var viewerId = TryGetUserId();
            var (items, total) = await _mediator.Send(new GetLatestFeedQuery(page, pageSize));
            var responses = new List<PostResponse>();
            foreach (var p in items)
            {
                var reactions = await _mediator.Send(new GetPostReactionsQuery(p.Id, viewerId));
                responses.Add(MapToPostResponse(p, reactions));
            }
            return Ok(new PagedResult<PostResponse>(responses, page, pageSize, total));
        }

        // ======================
        // GET: Post theo Tag
        // ======================
        [HttpGet("tag/{tagId:guid}")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetByTag(
            Guid tagId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var viewerId = TryGetUserId();
            var result = await _tags.GetPostsByTagAsync(tagId, viewerId, page, pageSize);
            return Ok(result);
        }

        // ======================
        // GET: Related posts
        // ======================
        [HttpGet("{id:guid}/related")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetRelated(
            Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = TryGetUserId();
            var post = await _mediator.Send(new GetPostByIdQuery(id, null));
            if (post is null)
                return NotFound(new { code = "POST_NOT_FOUND", message = "Bài viết không tồn tại hoặc đã bị xoá." });

            var relatedPosts = new List<PostResponse>();

            // Strategy 1: Get posts with same tags
            var tagIds = post.Tags.Select(t => t.Id).ToList();
            if (tagIds.Any())
            {
                foreach (var tagId in tagIds)
                {
                    var tagPosts = await _tags.GetPostsByTagAsync(tagId, userId, 1, pageSize * 2);
                    foreach (var p in tagPosts.Items.Where(p => p.Id != id))
                    {
                        if (!relatedPosts.Any(rp => rp.Id == p.Id))
                            relatedPosts.Add(p);
                    }
                }
            }

            // Strategy 2: Semantic search if not enough
            if (relatedPosts.Count < pageSize && !string.IsNullOrEmpty(post.Caption))
            {
                var semanticResult = await _search.SemanticSearchAsync(
                    new SemanticSearchRequest(post.Caption, 1, pageSize, 50),
                    userId ?? Guid.Empty);

                foreach (var searchPost in semanticResult.Posts.Where(p => p.Id != id))
                {
                    if (!relatedPosts.Any(rp => rp.Id == searchPost.Id))
                    {
                        var relatedPost = await _mediator.Send(new GetPostByIdQuery(searchPost.Id, userId));
                        if (relatedPost is not null)
                        {
                            var reactions = await _mediator.Send(new GetPostReactionsQuery(searchPost.Id, userId));
                            relatedPosts.Add(MapToPostResponse(relatedPost, reactions));
                        }
                    }
                }
            }

            relatedPosts.RemoveAll(p => p.Id == id);

            var paginated = relatedPosts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new PagedResult<PostResponse>(paginated, page, pageSize, relatedPosts.Count));
        }

        // ======================
        // POST: Tạo post
        // ======================
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<PostResponse>> Create(
            [FromForm] CreatePostRequest dto, [FromForm] List<IFormFile>? mediaFiles)
        {
            var authorId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(dto.Caption) && (dto.Tags == null || !dto.Tags.Any()))
                return BadRequest(new { code = "EMPTY_POST", message = "Bài viết trống. Cần có caption hoặc ít nhất 1 tag." });

            try
            {
                var uploadedItems = new List<UploadedMediaItem>();
                if (mediaFiles is { Count: > 0 })
                {
                    foreach (var file in mediaFiles)
                    {
                        var uploaded = await _cloudinary.UploadAsyncOrThrow(file, folder: "favi_posts");
                        uploadedItems.Add(new UploadedMediaItem(
                            uploaded.Url, uploaded.ThumbnailUrl, uploaded.PublicId,
                            uploaded.Width, uploaded.Height, uploaded.Format));
                    }
                }

                var result = await _mediator.Send(new CreatePostCommand(
                    AuthorId: authorId,
                    Caption: dto.Caption,
                    Privacy: (PostPrivacy)(int)(dto.PrivacyLevel),
                    LocationName: dto.Location?.Name,
                    LocationFullAddress: dto.Location?.FullAddress,
                    LocationLatitude: dto.Location?.Latitude,
                    LocationLongitude: dto.Location?.Longitude,
                    TagNames: dto.Tags?.ToList(),
                    MediaItems: uploadedItems
                ));

                if (!result.Success)
                    return BadRequest(new { code = result.ErrorCode, message = result.ErrorMessage });

                var created = await _mediator.Send(new GetPostByIdQuery(result.PostId!.Value, authorId));
                if (created is null)
                    return StatusCode(500, new { code = "POST_CREATION_FAILED", message = "Failed to reload created post." });

                var reactions = await _mediator.Send(new GetPostReactionsQuery(result.PostId.Value, authorId));
                return CreatedAtAction(nameof(GetById), new { id = result.PostId }, MapToPostResponse(created, reactions));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to upload"))
            {
                return BadRequest(new { code = "MEDIA_UPLOAD_FAILED", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = "POST_CREATION_FAILED", message = "Failed to create post.", details = ex.Message });
            }
        }

        // ======================
        // PUT: Cập nhật caption
        // ======================
        [Authorize]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdatePostRequest dto)
        {
            var requesterId = User.GetUserId();
            var result = await _mediator.Send(new UpdatePostCommand(id, requesterId, dto.Caption, null));
            return result.Success
                ? Ok(new { message = "Đã cập nhật bài viết." })
                : StatusCode(403, new { code = result.ErrorCode ?? "POST_FORBIDDEN_OR_NOT_FOUND", message = result.ErrorMessage ?? "Không thể chỉnh sửa bài viết." });
        }

        // ======================
        // DELETE: Xoá post (soft delete)
        // ======================
        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var requesterId = User.GetUserId();
            var result = await _mediator.Send(new DeletePostCommand(id, requesterId));
            return result.Success
                ? Ok(new { message = "Bài viết đã được chuyển vào thùng rác." })
                : StatusCode(403, new { code = result.ErrorCode ?? "POST_FORBIDDEN_OR_NOT_FOUND", message = result.ErrorMessage ?? "Không thể xoá bài viết." });
        }

        // ======================
        // POST: Restore from recycle bin
        // ======================
        [Authorize]
        [HttpPost("{id:guid}/restore")]
        public async Task<IActionResult> Restore(Guid id)
        {
            var requesterId = User.GetUserId();
            var result = await _mediator.Send(new RestorePostCommand(id, requesterId));
            return result.Success
                ? Ok(new { message = "Bài viết đã được khôi phục." })
                : StatusCode(403, new { code = result.ErrorCode ?? "POST_RESTORE_FAILED", message = result.ErrorMessage ?? "Không thể khôi phục bài viết." });
        }

        // ======================
        // DELETE: Permanently delete
        // ======================
        [Authorize]
        [HttpDelete("{id:guid}/permanent")]
        public async Task<IActionResult> PermanentDelete(Guid id)
        {
            var requesterId = User.GetUserId();
            var result = await _mediator.Send(new PermanentDeletePostCommand(id, requesterId));
            return result.Success
                ? Ok(new { message = "Bài viết đã được xoá vĩnh viễn." })
                : StatusCode(403, new { code = result.ErrorCode ?? "POST_PERMANENT_DELETE_FAILED", message = result.ErrorMessage ?? "Không thể xoá vĩnh viễn bài viết." });
        }

        // ======================
        // GET: Recycle bin
        // ======================
        [Authorize]
        [HttpGet("recycle-bin")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetRecycleBin(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.GetUserId();
            var (items, total) = await _mediator.Send(new GetRecycleBinQuery(userId, page, pageSize));
            var responses = new List<PostResponse>();
            foreach (var p in items)
            {
                var reactions = await _mediator.Send(new GetPostReactionsQuery(p.Id, userId));
                responses.Add(MapToPostResponse(p, reactions));
            }
            return Ok(new PagedResult<PostResponse>(responses, page, pageSize, total));
        }

        // ======================
        // POST: Archive
        // ======================
        [Authorize]
        [HttpPost("{id:guid}/archive")]
        public async Task<IActionResult> Archive(Guid id)
        {
            var requesterId = User.GetUserId();
            var result = await _mediator.Send(new ArchivePostCommand(id, requesterId, true));
            return result.Success
                ? Ok(new { message = "Bài viết đã được lưu trữ." })
                : StatusCode(403, new { code = result.ErrorCode ?? "POST_ARCHIVE_FAILED", message = result.ErrorMessage ?? "Không thể lưu trữ bài viết." });
        }

        // ======================
        // POST: Unarchive
        // ======================
        [Authorize]
        [HttpPost("{id:guid}/unarchive")]
        public async Task<IActionResult> Unarchive(Guid id)
        {
            var requesterId = User.GetUserId();
            var result = await _mediator.Send(new ArchivePostCommand(id, requesterId, false));
            return result.Success
                ? Ok(new { message = "Bài viết đã được bỏ lưu trữ." })
                : StatusCode(403, new { code = result.ErrorCode ?? "POST_UNARCHIVE_FAILED", message = result.ErrorMessage ?? "Không thể bỏ lưu trữ bài viết." });
        }

        // ======================
        // GET: Archived posts
        // ======================
        [Authorize]
        [HttpGet("archived")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetArchived(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.GetUserId();
            var (items, total) = await _mediator.Send(new GetArchivedPostsQuery(userId, page, pageSize));
            var responses = new List<PostResponse>();
            foreach (var p in items)
            {
                var reactions = await _mediator.Send(new GetPostReactionsQuery(p.Id, userId));
                responses.Add(MapToPostResponse(p, reactions));
            }
            return Ok(new PagedResult<PostResponse>(responses, page, pageSize, total));
        }

        // ======================
        // POST: Upload Media
        // ======================
        [Authorize]
        [HttpPost("{id:guid}/media")]
        public async Task<ActionResult<IEnumerable<PostMediaResponse>>> UploadMedia(
            Guid id, [FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest(new { code = "NO_FILE", message = "Không có file nào được gửi." });

            var requesterId = User.GetUserId();
            var post = await _mediator.Send(new GetPostByIdQuery(id, requesterId));
            if (post is null)
                return NotFound(new { code = "POST_NOT_FOUND", message = "Bài viết không tồn tại." });

            if (post.AuthorProfileId != requesterId)
                return StatusCode(403, new { code = "NOT_OWNER", message = "Chỉ chủ bài viết mới được upload media." });

            var uploadedItems = new List<UploadedMediaItem>();
            var mediaResponses = new List<PostMediaResponse>();

            foreach (var file in files)
            {
                var uploaded = await _cloudinary.UploadAsyncOrThrow(file, folder: "favi_posts");
                uploadedItems.Add(new UploadedMediaItem(
                    uploaded.Url, uploaded.ThumbnailUrl, uploaded.PublicId,
                    uploaded.Width, uploaded.Height, uploaded.Format));
                mediaResponses.Add(new PostMediaResponse(
                    Guid.NewGuid(), id, uploaded.Url, uploaded.PublicId,
                    uploaded.Width, uploaded.Height, uploaded.Format,
                    post.Medias.Count + mediaResponses.Count,
                    uploaded.ThumbnailUrl));
            }

            var result = await _mediator.Send(new AddPostMediaCommand(id, requesterId, uploadedItems));
            if (!result.Success)
                return BadRequest(new { code = result.ErrorCode, message = result.ErrorMessage });

            return Ok(mediaResponses);
        }

        // ======================
        // POST: Toggle Reaction
        // ======================
        [Authorize]
        [HttpPost("{id:guid}/reactions")]
        public async Task<ActionResult> ToggleReaction(Guid id, [FromQuery] string type)
        {
            var userId = User.GetUserId();

            if (!Enum.TryParse<ReactionType>(type, true, out var legacyType))
                return BadRequest(new { code = "INVALID_REACTION_TYPE", message = $"Giá trị reaction '{type}' không hợp lệ." });

            var result = await _mediator.Send(new TogglePostReactionCommand(
                id, userId, (EngagementReactionType)(int)legacyType));

            if (!result.IsSuccess)
                return NotFound(new { code = result.ErrorCode, message = result.ErrorMessage });

            if (result.Removed)
                return Ok(new { removed = true, message = "Reaction đã được gỡ." });

            return Ok(new { type = result.Type!.ToString(), message = "Reaction đã được cập nhật." });
        }

        // ======================
        // GET: Reactors
        // ======================
        [Authorize]
        [HttpGet("{id:guid}/reactors")]
        public async Task<ActionResult<IEnumerable<PostReactorResponse>>> GetReactors(Guid id)
        {
            var reactors = await _mediator.Send(new GetPostReactorsQuery(id));
            return Ok(reactors.Select(r => new PostReactorResponse(
                r.ProfileId,
                r.Username,
                r.DisplayName,
                r.AvatarUrl,
                (ReactionType)(int)r.ReactionType,
                r.ReactedAt)));
        }

        // ======================
        // POST: Share/Repost
        // ======================
        [Authorize]
        [HttpPost("{id:guid}/share")]
        public async Task<ActionResult<RepostResponse>> SharePost(Guid id, [FromBody] CreateRepostRequest dto)
        {
            var userId = User.GetUserId();

            var postExists = await _mediator.Send(new GetPostByIdQuery(id, userId));
            if (postExists is null)
                return NotFound(new { code = "POST_NOT_FOUND", message = "Bài viết không tồn tại." });

            var result = await _mediator.Send(new SharePostCommand(id, userId, dto.Caption));
            if (!result.Success)
                return StatusCode(403, new { code = result.ErrorCode, message = result.ErrorMessage ?? "Không thể chia sẻ bài viết này." });

            var repost = await _mediator.Send(new GetRepostByIdQuery(result.RepostId!.Value, userId));
            if (repost is null)
                return StatusCode(500, new { code = "REPOST_LOAD_FAILED", message = "Failed to reload repost." });

            var reactions = await _mediator.Send(new GetPostReactionsQuery(repost.OriginalPostId, userId));
            return Ok(MapToRepostResponse(repost, reactions));
        }

        // ======================
        // DELETE: Unshare
        // ======================
        [Authorize]
        [HttpDelete("{id:guid}/share")]
        public async Task<IActionResult> UnsharePost(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _mediator.Send(new UnsharePostCommand(id, userId));
            return result.Success
                ? Ok(new { message = "Đã bỏ chia sẻ bài viết." })
                : NotFound(new { code = result.ErrorCode, message = result.ErrorMessage ?? "Bạn chưa chia sẻ bài viết này." });
        }

        // ======================
        // GET: Get reposts by profile
        // ======================
        [HttpGet("profile/{profileId:guid}/shares")]
        public async Task<ActionResult<PagedResult<RepostResponse>>> GetProfileShares(
            Guid profileId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var currentUserId = TryGetUserId();
            var (items, total) = await _mediator.Send(new GetRepostsByProfileQuery(profileId, currentUserId, page, pageSize));
            var responses = new List<RepostResponse>();
            foreach (var r in items)
            {
                var reactions = await _mediator.Send(new GetPostReactionsQuery(r.OriginalPostId, currentUserId));
                responses.Add(MapToRepostResponse(r, reactions));
            }
            return Ok(new PagedResult<RepostResponse>(responses, page, pageSize, total));
        }

        // ======================
        // GET: Get repost by ID
        // ======================
        [HttpGet("shares/{repostId:guid}")]
        public async Task<ActionResult<RepostResponse>> GetRepost(Guid repostId)
        {
            var currentUserId = TryGetUserId();
            var repost = await _mediator.Send(new GetRepostByIdQuery(repostId, currentUserId));
            if (repost is null)
                return NotFound(new { code = "REPOST_NOT_FOUND", message = "Bài chia sẻ không tồn tại." });

            var reactions = await _mediator.Send(new GetPostReactionsQuery(repost.OriginalPostId, currentUserId));
            return Ok(MapToRepostResponse(repost, reactions));
        }

        // ── Mappers ──────────────────────────────────────────────────────────

        private static PostResponse MapToPostResponse(PostReadModel post,
            Favi_BE.Modules.Engagement.Application.Contracts.ReadModels.ReactionSummaryQueryDto reactions)
        {
            var byType = reactions.ByType.ToDictionary(
                kvp => (ReactionType)(int)kvp.Key,
                kvp => kvp.Value);
            ReactionType? current = reactions.CurrentUserReaction is { } t ? (ReactionType)(int)t : null;

            return new PostResponse(
                post.Id,
                post.AuthorProfileId,
                post.Caption,
                post.CreatedAt,
                post.UpdatedAt,
                (PrivacyLevel)post.Privacy,
                post.Medias.Select(m => new PostMediaResponse(
                    m.Id, m.PostId, m.Url, m.PublicId ?? string.Empty,
                    m.Width, m.Height, m.Format ?? string.Empty, m.Position, m.ThumbnailUrl)),
                post.Tags.Select(t => new TagDto(t.Id, t.Name)),
                new ReactionSummaryDto(reactions.Total, byType, current),
                post.CommentsCount,
                post.Location is not null
                    ? new LocationDto(post.Location.Name, post.Location.FullAddress, post.Location.Latitude, post.Location.Longitude)
                    : null,
                post.IsNSFW);
        }

        private static RepostResponse MapToRepostResponse(
            RepostReadModel repost,
            Favi_BE.Modules.Engagement.Application.Contracts.ReadModels.ReactionSummaryQueryDto reactions)
        {
            var byType = reactions.ByType.ToDictionary(
                kvp => (ReactionType)(int)kvp.Key,
                kvp => kvp.Value);
            ReactionType? current = reactions.CurrentUserReaction is { } t ? (ReactionType)(int)t : null;

            return new RepostResponse(
                repost.Id,
                repost.ProfileId,
                repost.Username,
                repost.DisplayName,
                repost.AvatarUrl,
                repost.OriginalPostId,
                repost.OriginalCaption,
                repost.OriginalAuthorProfileId,
                repost.OriginalAuthorUsername,
                repost.OriginalAuthorDisplayName,
                repost.OriginalAuthorAvatarUrl,
                repost.OriginalPostMedias.Select(m => new PostMediaResponse(
                    m.Id, m.PostId, m.Url, m.PublicId ?? string.Empty,
                    m.Width, m.Height, m.Format ?? string.Empty, m.Position, m.ThumbnailUrl)),
                repost.Caption,
                repost.CreatedAt,
                repost.UpdatedAt,
                repost.CommentsCount,
                new ReactionSummaryDto(reactions.Total, byType, current),
                repost.RepostsCount,
                repost.IsRepostedByCurrentUser);
        }
    }
}

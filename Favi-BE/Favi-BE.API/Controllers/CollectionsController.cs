using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts.ReadModels;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetCollectionById;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetCollectionPosts;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetCollections;
using Favi_BE.Modules.ContentDiscovery.Application.Queries.GetTrendingCollections;
using Favi_BE.Modules.ContentPublishing.Application.Commands.AddPostToCollection;
using Favi_BE.Modules.ContentPublishing.Application.Commands.CreateCollection;
using Favi_BE.Modules.ContentPublishing.Application.Commands.DeleteCollection;
using Favi_BE.Modules.ContentPublishing.Application.Commands.RemovePostFromCollection;
using Favi_BE.Modules.ContentPublishing.Application.Commands.UpdateCollection;
using Favi_BE.Modules.ContentPublishing.Domain;
using Favi_BE.Modules.Engagement.Application.Commands.ToggleCollectionReaction;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using Favi_BE.Modules.Engagement.Application.Queries.GetCollectionReactions;
using Favi_BE.Modules.Engagement.Application.Queries.GetCollectionReactors;
using Favi_BE.Modules.Engagement.Application.Queries.GetPostReactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EngagementReactionType = Favi_BE.Modules.Engagement.Domain.ReactionType;
using LegacyPrivacyLevel = Favi_BE.Models.Enums.PrivacyLevel;
using LegacyReactionType = Favi_BE.Models.Enums.ReactionType;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CollectionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICloudinaryService _cloudinary;

        public CollectionsController(IMediator mediator, ICloudinaryService cloudinary)
        {
            _mediator = mediator;
            _cloudinary = cloudinary;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CollectionResponse>> Create([FromForm] CreateCollectionRequest dto, [FromForm] IFormFile? coverImage)
        {
            var userId = User.GetUserId();

            string? coverImageUrl = null, coverImagePublicId = null;
            if (coverImage is { Length: > 0 })
            {
                if (!coverImage.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = "Cover image must be an image file." });
                var uploaded = await _cloudinary.TryUploadAsync(coverImage, CancellationToken.None, "favi_collections");
                if (uploaded is null)
                    return StatusCode(500, new { message = "Failed to upload cover image." });
                coverImageUrl = uploaded.Url;
                coverImagePublicId = uploaded.PublicId;
            }

            var result = await _mediator.Send(new CreateCollectionCommand(
                userId, dto.Title, dto.Description,
                (CollectionPrivacy)(int)dto.PrivacyLevel, coverImageUrl, coverImagePublicId));

            if (!result.Success)
                return BadRequest(new { code = result.ErrorCode, message = result.ErrorMessage });

            var collection = await _mediator.Send(new GetCollectionByIdQuery(result.CollectionId!.Value, userId));
            if (collection is null)
                return StatusCode(500, new { message = "Collection created but could not be retrieved." });

            var reactions = await _mediator.Send(new GetCollectionReactionsQuery(collection.Id, userId));
            return Ok(MapToCollectionResponse(collection, reactions));
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<CollectionResponse>> Update(Guid id, [FromForm] UpdateCollectionRequest dto, [FromForm] IFormFile? coverImage)
        {
            var userId = User.GetUserId();

            string? coverImageUrl = null, coverImagePublicId = null;
            if (coverImage is { Length: > 0 })
            {
                if (!coverImage.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = "Cover image must be an image file." });
                var uploaded = await _cloudinary.TryUploadAsync(coverImage, CancellationToken.None, "favi_collections");
                if (uploaded is null)
                    return StatusCode(500, new { message = "Failed to upload cover image." });
                coverImageUrl = uploaded.Url;
                coverImagePublicId = uploaded.PublicId;
            }

            CollectionPrivacy? privacy = dto.PrivacyLevel.HasValue
                ? (CollectionPrivacy)(int)dto.PrivacyLevel.Value
                : null;

            var result = await _mediator.Send(new UpdateCollectionCommand(
                id, userId, dto.Title, dto.Description, privacy, coverImageUrl, coverImagePublicId));

            if (!result.Success)
                return result.ErrorCode == "COLLECTION_NOT_FOUND"
                    ? NotFound(new { code = result.ErrorCode, message = result.ErrorMessage })
                    : StatusCode(403, new { code = result.ErrorCode, message = result.ErrorMessage });

            var collection = await _mediator.Send(new GetCollectionByIdQuery(id, userId));
            if (collection is null)
                return NotFound(new { code = "COLLECTION_NOT_FOUND_OR_FORBIDDEN", message = "Không tìm thấy hoặc bạn không có quyền sửa bộ sưu tập này." });

            var reactions = await _mediator.Send(new GetCollectionReactionsQuery(collection.Id, userId));
            return Ok(MapToCollectionResponse(collection, reactions));
        }

        [HttpGet("{id}/posts")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetPosts(Guid id, int page = 1, int pageSize = 20)
        {
            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
            var collection = await _mediator.Send(new GetCollectionByIdQuery(id, viewerId));
            if (collection is null)
                return NotFound(new { code = "COLLECTION_NOT_FOUND", message = "Bộ sưu tập không tồn tại." });

            var (posts, total) = await _mediator.Send(new GetCollectionPostsQuery(id, viewerId, page, pageSize));
            var dtos = new List<PostResponse>();
            foreach (var p in posts)
            {
                var reactions = await _mediator.Send(new GetPostReactionsQuery(p.Id, viewerId));
                dtos.Add(MapToPostResponse(p, reactions));
            }
            return Ok(new PagedResult<PostResponse>(dtos, page, pageSize, total));
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _mediator.Send(new DeleteCollectionCommand(id, userId));
            return result.Success
                ? NoContent()
                : StatusCode(403, new { code = "NOT_OWNER", message = "Chỉ chủ sở hữu mới được xoá bộ sưu tập." });
        }

        [HttpGet("owner/{ownerId}")]
        public async Task<ActionResult<PagedResult<CollectionResponse>>> GetByOwner(Guid ownerId, int page = 1, int pageSize = 20)
        {
            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
            var (collections, total) = await _mediator.Send(new GetCollectionsQuery(ownerId, viewerId, page, pageSize));
            var dtos = new List<CollectionResponse>();
            foreach (var c in collections)
            {
                var reactions = await _mediator.Send(new GetCollectionReactionsQuery(c.Id, viewerId));
                dtos.Add(MapToCollectionResponse(c, reactions));
            }
            return Ok(new PagedResult<CollectionResponse>(dtos, page, pageSize, total));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CollectionResponse>> GetById(Guid id)
        {
            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
            var collection = await _mediator.Send(new GetCollectionByIdQuery(id, viewerId));
            if (collection is null)
                return NotFound(new { code = "COLLECTION_NOT_FOUND", message = "Bộ sưu tập không tồn tại." });
            var reactions = await _mediator.Send(new GetCollectionReactionsQuery(id, viewerId));
            return Ok(MapToCollectionResponse(collection, reactions));
        }

        [Authorize]
        [HttpPost("{id}/posts/{postId}")]
        public async Task<IActionResult> AddPost(Guid id, Guid postId)
        {
            var userId = User.GetUserId();
            var result = await _mediator.Send(new AddPostToCollectionCommand(id, postId, userId));
            return result.Success
                ? Ok(new { message = "Đã thêm bài viết vào bộ sưu tập." })
                : StatusCode(403, new { code = "NOT_OWNER_OR_INVALID", message = "Không thể thêm: bạn không sở hữu hoặc dữ liệu không hợp lệ." });
        }

        [Authorize]
        [HttpDelete("{id}/posts/{postId}")]
        public async Task<IActionResult> RemovePost(Guid id, Guid postId)
        {
            var userId = User.GetUserId();
            var result = await _mediator.Send(new RemovePostFromCollectionCommand(id, postId, userId));
            return result.Success
                ? NoContent()
                : StatusCode(403, new { code = "NOT_OWNER_OR_INVALID", message = "Không thể xoá khỏi bộ sưu tập." });
        }

        [Authorize]
        [HttpPost("{id}/reactions")]
        public async Task<ActionResult> ToggleReaction(Guid id, [FromQuery] string type)
        {
            var userId = User.GetUserId();

            if (!Enum.TryParse<LegacyReactionType>(type, true, out var legacyType))
                return BadRequest(new { code = "INVALID_REACTION_TYPE", message = "Loại reaction không hợp lệ." });

            var result = await _mediator.Send(new ToggleCollectionReactionCommand(
                id, userId, (EngagementReactionType)(int)legacyType));

            if (!result.IsSuccess)
                return NotFound(new { code = result.ErrorCode, message = result.ErrorMessage });

            if (result.Removed)
                return Ok(new { removed = true, message = "Reaction đã được gỡ." });

            return Ok(new { type = result.Type!.ToString(), message = "Reaction đã được cập nhật." });
        }

        [Authorize]
        [HttpGet("{id:guid}/reactors")]
        public async Task<ActionResult<IEnumerable<CollectionReactorResponse>>> GetReactors(Guid id)
        {
            var reactors = await _mediator.Send(new GetCollectionReactorsQuery(id));
            return Ok(reactors.Select(r => new CollectionReactorResponse(
                r.ProfileId,
                r.Username,
                r.DisplayName,
                r.AvatarUrl,
                (LegacyReactionType)(int)r.ReactionType,
                r.ReactedAt)));
        }

        [HttpGet("trending")]
        public async Task<ActionResult<PagedResult<CollectionResponse>>> GetTrending(int page = 1, int pageSize = 20)
        {
            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
            var (collections, total) = await _mediator.Send(new GetTrendingCollectionsQuery(viewerId, page, pageSize));
            var dtos = new List<CollectionResponse>();
            foreach (var c in collections)
            {
                var reactions = await _mediator.Send(new GetCollectionReactionsQuery(c.Id, viewerId));
                dtos.Add(MapToCollectionResponse(c, reactions));
            }
            return Ok(new PagedResult<CollectionResponse>(dtos, page, pageSize, total));
        }

        // ── Mapping helpers ───────────────────────────────────────────────

        private static CollectionResponse MapToCollectionResponse(CollectionReadModel c, ReactionSummaryQueryDto reactions)
        {
            var byType = reactions.ByType.ToDictionary(
                kvp => (LegacyReactionType)(int)kvp.Key, kvp => kvp.Value);
            LegacyReactionType? mine = reactions.CurrentUserReaction is { } t
                ? (LegacyReactionType)(int)t
                : null;
            var summary = new ReactionSummaryDto(reactions.Total, byType, mine);

            return new CollectionResponse(
                c.Id,
                c.OwnerProfileId,
                c.Title,
                c.Description,
                c.CoverImageUrl ?? string.Empty,
                (LegacyPrivacyLevel)c.Privacy,
                c.CreatedAt,
                c.UpdatedAt,
                c.PostIds,
                c.PostCount,
                summary);
        }

        private static PostResponse MapToPostResponse(PostReadModel post, ReactionSummaryQueryDto reactions)
        {
            var byType = reactions.ByType.ToDictionary(
                kvp => (LegacyReactionType)(int)kvp.Key, kvp => kvp.Value);
            LegacyReactionType? mine = reactions.CurrentUserReaction is { } t
                ? (LegacyReactionType)(int)t
                : null;
            var summary = new ReactionSummaryDto(reactions.Total, byType, mine);

            var medias = post.Medias.Select(m => new PostMediaResponse(
                m.Id, m.PostId, m.Url, m.PublicId ?? string.Empty,
                m.Width, m.Height, m.Format ?? string.Empty, m.Position, m.ThumbnailUrl));

            var tags = post.Tags.Select(t => new TagDto(t.Id, t.Name));

            LocationDto? location = post.Location is { } loc
                ? new LocationDto(loc.Name, loc.FullAddress, loc.Latitude, loc.Longitude)
                : null;

            return new PostResponse(
                post.Id,
                post.AuthorProfileId,
                post.Caption,
                post.CreatedAt,
                post.UpdatedAt,
                (LegacyPrivacyLevel)post.Privacy,
                medias,
                tags,
                summary,
                post.CommentsCount,
                location,
                post.IsNSFW);
        }
    }
}

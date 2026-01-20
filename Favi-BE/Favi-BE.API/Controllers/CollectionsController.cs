using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CollectionsController : ControllerBase
    {
        private readonly ICollectionService _collections;
        private readonly IPrivacyGuard _privacy;
        public CollectionsController(ICollectionService collections, IPrivacyGuard privacyGuard) { _collections = collections; _privacy = privacyGuard; }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CollectionResponse>> Create([FromForm] CreateCollectionRequest dto, [FromForm] IFormFile? coverImage)
        {
            var userId = User.GetUserId();
            return Ok(await _collections.CreateAsync(userId, dto, coverImage));
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<CollectionResponse>> Update(Guid id, [FromForm] UpdateCollectionRequest dto, [FromForm] IFormFile? coverImage)
        {
            var userId = User.GetUserId();
            var updated = await _collections.UpdateAsync(id, userId, dto, coverImage);
            return updated is null
                ? NotFound(new { code = "COLLECTION_NOT_FOUND_OR_FORBIDDEN", message = "Không tìm thấy hoặc bạn không có quyền sửa bộ sưu tập này." })
                : Ok(updated);
        }

        [HttpGet("{id}/posts")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetPosts(Guid id, int page = 1, int pageSize = 20)
        {
            var collection = await _collections.GetEntityByIdAsync(id);
            if (collection is null)
                return NotFound(new { code = "COLLECTION_NOT_FOUND", message = "Bộ sưu tập không tồn tại." });

            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
            if (!await _privacy.CanViewCollectionAsync(collection, viewerId))
                return StatusCode(403, new { code = "COLLECTION_FORBIDDEN", message = "Bạn không có quyền xem bộ sưu tập này." });

            return Ok(await _collections.GetPostsAsync(id, page, pageSize));
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserId();
            var ok = await _collections.DeleteAsync(id, userId);
            return ok
                ? NoContent()
                : StatusCode(403, new { code = "NOT_OWNER", message = "Chỉ chủ sở hữu mới được xoá bộ sưu tập." });
        }

        [HttpGet("owner/{ownerId}")]
        public async Task<ActionResult<PagedResult<CollectionResponse>>> GetByOwner(Guid ownerId, int page = 1, int pageSize = 20)
        {
            var currentUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
            return Ok(await _collections.GetByOwnerAsync(ownerId, page, pageSize, currentUserId));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CollectionResponse>> GetById(Guid id)
        {
            var collection = await _collections.GetEntityByIdAsync(id);
            if (collection is null)
                return NotFound(new { code = "COLLECTION_NOT_FOUND", message = "Bộ sưu tập không tồn tại." });

            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
            if (!await _privacy.CanViewCollectionAsync(collection, viewerId))
                return StatusCode(403, new { code = "COLLECTION_FORBIDDEN", message = "Bạn không có quyền xem bộ sưu tập này." });

            return Ok(await _collections.GetByIdAsync(id, viewerId));
        }
        [Authorize]
        [HttpPost("{id}/posts/{postId}")]
        public async Task<IActionResult> AddPost(Guid id, Guid postId)
        {
            var userId = User.GetUserId();
            var ok = await _collections.AddPostAsync(id, postId, userId);
            return ok
                ? Ok(new { message = "Đã thêm bài viết vào bộ sưu tập." })
                : StatusCode(403, new { code = "NOT_OWNER_OR_INVALID", message = "Không thể thêm: bạn không sở hữu hoặc dữ liệu không hợp lệ." });
        }

        [Authorize]
        [HttpDelete("{id}/posts/{postId}")]
        public async Task<IActionResult> RemovePost(Guid id, Guid postId)
        {
            var userId = User.GetUserId();
            var ok = await _collections.RemovePostAsync(id, postId, userId);
            return ok
                ? NoContent()
                : StatusCode(403, new { code = "NOT_OWNER_OR_INVALID", message = "Không thể xoá khỏi bộ sưu tập." });
        }

        [Authorize]
        [HttpPost("{id}/reactions")]
        public async Task<ActionResult> ToggleReaction(Guid id, [FromQuery] string type)
        {
            var userId = User.GetUserId();

            if (!Enum.TryParse<Models.Enums.ReactionType>(type, true, out var reactionType))
                return BadRequest(new { code = "INVALID_REACTION_TYPE", message = "Loại reaction không hợp lệ." });

            var collection = await _collections.GetEntityByIdAsync(id);
            if (collection is null)
                return NotFound(new { code = "COLLECTION_NOT_FOUND", message = "Bộ sưu tập không tồn tại." });

            var newState = await _collections.ToggleReactionAsync(id, userId, reactionType);

            if (newState is null)
                return Ok(new { removed = true, message = "Reaction đã được gỡ." });

            return Ok(new { type = newState.ToString(), message = "Reaction đã được cập nhật." });
        }

        [Authorize]
        [HttpGet("{id:guid}/reactors")]
        public async Task<ActionResult<IEnumerable<CollectionReactorResponse>>> GetReactors(Guid id)
        {
            var userId = User.GetUserId();

            try
            {
                var reactors = await _collections.GetReactorsAsync(id, userId);
                return Ok(reactors);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("trending")]
        public async Task<ActionResult<PagedResult<CollectionResponse>>> GetTrending(int page = 1, int pageSize = 20)
        {
            var currentUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
            return Ok(await _collections.GetTrendingCollectionsAsync(page, pageSize, currentUserId));
        }
    }
}
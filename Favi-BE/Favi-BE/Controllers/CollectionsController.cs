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
        public CollectionsController(ICollectionService collections, IPrivacyGuard privacyGuard) { _collections = collections; _privacy = privacyGuard}

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CollectionResponse>> Create(CreateCollectionRequest dto)
        {
            var userId = User.GetUserIdFromMetadata();
            return Ok(await _collections.CreateAsync(userId, dto));
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<CollectionResponse>> Update(Guid id, UpdateCollectionRequest dto)
        {
            var userId = User.GetUserIdFromMetadata();
            return Ok(await _collections.UpdateAsync(id, userId, dto));
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserIdFromMetadata();
            var ok = await _collections.DeleteAsync(id, userId);
            return ok ? NoContent() : Forbid();
        }

        [HttpGet("owner/{ownerId}")]
        public async Task<ActionResult<PagedResult<CollectionResponse>>> GetByOwner(Guid ownerId, int page = 1, int pageSize = 20)
        {
            return Ok(await _collections.GetByOwnerAsync(ownerId, page, pageSize));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CollectionResponse>> GetById(Guid id)
        {
            var collection = await _collections.GetEntityByIdAsync(id);
            if (collection is null)
                return NotFound();
            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserIdFromMetadata() : (Guid?)null;
            if (!await _privacy.CanViewCollectionAsync(collection, viewerId))
                return Forbid();
            return Ok(await _collections.GetByIdAsync(id));
        }
        [Authorize]
        [HttpPost("{id}/posts/{postId}")]
        public async Task<IActionResult> AddPost(Guid id, Guid postId)
        {
            var userId = User.GetUserIdFromMetadata();
            var ok = await _collections.AddPostAsync(id, postId, userId);
            return ok ? Ok() : Forbid();
        }

        [Authorize]
        [HttpDelete("{id}/posts/{postId}")]
        public async Task<IActionResult> RemovePost(Guid id, Guid postId)
        {
            var userId = User.GetUserIdFromMetadata();
            var ok = await _collections.RemovePostAsync(id, postId, userId);
            return ok ? NoContent() : Forbid();
        }

        [HttpGet("{id}/posts")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetPosts(Guid id, int page = 1, int pageSize = 20)
        {
            var collection = await _collections.GetEntityByIdAsync(id);
            if (collection is null)
                return NotFound();
            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserIdFromMetadata() : (Guid?)null;
            if (!await _privacy.CanViewCollectionAsync(collection, viewerId))
                return Forbid();
            return Ok(await _collections.GetPostsAsync(id, page, pageSize));
        }
    }
}
using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly IPostService _posts;
        private readonly ITagService _tags;
        private readonly IPrivacyGuard _privacy;
        private readonly IProfileService _profileService; 
        public PostsController(IPostService posts, ITagService tags, IPrivacyGuard privacy, IProfileService profileService)
        {
            _posts = posts;
            _tags = tags;
            _privacy = privacy;
            _profileService = profileService;
        }

        // ======================
        // 🔹 Helper
        // ======================
        private Guid? TryGetUserId()
        {
            if (User?.Identity?.IsAuthenticated != true) return null;
            try { return User.GetUserIdFromMetadata(); }
            catch { return null; }
        }

        // ======================
        // 🔹 GET: Chi tiết post
        // ======================
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PostResponse>> GetById(Guid id)
        {
            var viewerId = TryGetUserId();

            // Lấy entity gốc để guard privacy
            var postEntity = await _posts.GetEntityAsync(id);
            if (postEntity == null)
                return NotFound("Post không tồn tại.");

            // ✅ Check privacy
            if (!await _privacy.CanViewPostAsync(postEntity, viewerId))
                return Forbid();

            // Map sang response (service đã có)
            var post = await _posts.GetByIdAsync(id, viewerId);
            return Ok(post);
        }

        // ======================
        // 🔹 GET: Bài viết theo Profile (Public wall)
        // ======================
        [HttpGet("profile/{profileId:guid}")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetByProfile(Guid profileId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var viewerId = TryGetUserId();
            var profile = await _profileService.GetEntityByIdAsync(profileId);
            if(profile == null)
                return NotFound("Profile không tồn tại.");
            // ✅ Check xem có quyền xem profile không
            if (!await _privacy.CanViewProfileAsync(profile, viewerId))
                return Forbid();

            // Lấy danh sách post của profile
            var result = await _posts.GetByProfileAsync(profileId, viewerId, page, pageSize);

            // ✅ Lọc những bài mà viewer không có quyền xem
            var visiblePosts = new List<PostResponse>();
            foreach (var p in result.Items)
            {
                var entity = await _posts.GetEntityAsync(p.Id);
                if (await _privacy.CanViewPostAsync(entity, viewerId))
                    visiblePosts.Add(p);
            }

            return Ok(new PagedResult<PostResponse>(visiblePosts, page, pageSize, result.TotalCount));
        }

        // ======================
        // 🔹 GET: Feed cá nhân (posts của người dùng + followings)
        // ======================
        [Authorize]
        [HttpGet("feed")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetPersonalFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.GetUserIdFromMetadata();
            var result = await _posts.GetFeedAsync(userId, page, pageSize);

            // ✅ Lọc những post mà người xem không được phép xem (ví dụ private)
            var visiblePosts = new List<PostResponse>();
            foreach (var p in result.Items)
            {
                var entity = await _posts.GetEntityAsync(p.Id);
                if (await _privacy.CanViewPostAsync(entity, userId))
                    visiblePosts.Add(p);
            }

            return Ok(new PagedResult<PostResponse>(visiblePosts, page, pageSize, result.TotalCount));
        }

        // ======================
        // 🔹 GET: Explore (TODO - thuật toán đề xuất)
        // ======================
        [Authorize]
        [HttpGet("explore")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetExplore([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            // TODO: Thuật toán explore (gợi ý theo tag, độ tương tác, profile tương đồng, v.v.)
            return Ok(await _posts.GetExploreAsync(User.GetUserIdFromMetadata(), page, pageSize));
        }

        // ======================
        // 🔹 GET: Latest posts (TODO - thuật toán sort toàn hệ thống)
        // ======================
        [HttpGet("latest")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetLatest([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            // TODO: Lấy danh sách bài mới nhất toàn hệ thống
            return Ok(await _posts.GetLatestAsync(page, pageSize));
        }

        // ======================
        // 🔹 GET: Post theo Tag (gom từ TagController)
        // ======================
        [HttpGet("tag/{tagId:guid}")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetByTag(Guid tagId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var viewerId = TryGetUserId();
            var result = await _tags.GetPostsByTagAsync(tagId, page, pageSize);

            // ✅ Lọc quyền xem
            var visiblePosts = new List<PostResponse>();
            foreach (var p in result.Items)
            {
                var entity = await _posts.GetEntityAsync(p.Id);
                if (await _privacy.CanViewPostAsync(entity, viewerId))
                    visiblePosts.Add(p);
            }

            return Ok(new PagedResult<PostResponse>(visiblePosts, page, pageSize, result.TotalCount));
        }

        // ======================
        // 🔹 POST: Tạo post
        // ======================
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<PostResponse>> Create(CreatePostRequest dto)
        {
            var authorId = User.GetUserIdFromMetadata();
            if (string.IsNullOrWhiteSpace(dto.Caption) && (dto.Tags == null || !dto.Tags.Any()))
                return BadRequest("Bài viết trống.");

            var created = await _posts.CreateAsync(authorId, dto.Caption, dto.Tags);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // ======================
        // 🔹 PUT: Cập nhật caption
        // ======================
        [Authorize]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdatePostRequest dto)
        {
            var requesterId = User.GetUserIdFromMetadata();
            var ok = await _posts.UpdateAsync(id, requesterId, dto.Caption);
            return ok ? Ok() : Forbid("Không thể chỉnh sửa bài viết.");
        }

        // ======================
        // 🔹 DELETE: Xoá post (cascade tự lo)
        // ======================
        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var requesterId = User.GetUserIdFromMetadata();
            var ok = await _posts.DeleteAsync(id, requesterId);
            return ok ? NoContent() : Forbid("Không thể xoá bài viết.");
        }

        // ======================
        // 🔹 POST: Upload Media
        // ======================
        [Authorize]
        [HttpPost("{id:guid}/media")]
        public async Task<ActionResult<IEnumerable<PostMediaResponse>>> UploadMedia(Guid id, [FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("Không có file nào được gửi.");

            var requesterId = User.GetUserIdFromMetadata();
            var result = await _posts.UploadMediaAsync(id, files, requesterId);
            return Ok(result);
        }

        // ======================
        // 🔹 POST: Toggle Reaction
        // ======================
        [Authorize]
        [HttpPost("{id:guid}/reactions")]
        public async Task<ActionResult> ToggleReaction(Guid id, [FromQuery] string type)
        {
            var userId = User.GetUserIdFromMetadata();
            var parsed = Enum.TryParse<ReactionType>(type, true, out var reactionType);
            var ok = await _posts.ToggleReactionAsync(id, userId, reactionType);
            return ok != null ? Ok() : BadRequest("Không thể thay đổi reaction.");
        }
    }
}

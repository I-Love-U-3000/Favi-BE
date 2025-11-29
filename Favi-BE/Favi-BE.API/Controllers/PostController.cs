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
            var postEntity = await _posts.GetEntityAsync(id);
            if (postEntity == null)
                return NotFound(new { code = "POST_NOT_FOUND", message = "Bài viết không tồn tại hoặc đã bị xoá." }); // 👈 rõ ràng

            if (!await _privacy.CanViewPostAsync(postEntity, viewerId))
                return StatusCode(403, new { code = "POST_FORBIDDEN", message = "Bạn không có quyền xem bài viết này." }); // 👈 có body

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
            if (profile == null)
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Hồ sơ không tồn tại." });

            if (!await _privacy.CanViewProfileAsync(profile, viewerId))
                return StatusCode(403, new { code = "PROFILE_FORBIDDEN", message = "Bạn không có quyền xem bài viết của hồ sơ này." });

            var result = await _posts.GetByProfileAsync(profileId, viewerId, page, pageSize);
            var visiblePosts = new List<PostResponse>();
            foreach (var p in result.Items)
            {
                var entity = await _posts.GetEntityAsync(p.Id);
                if (entity == null) continue;
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
        // 🔹 GET: Feed cá nhân (cho Guest)
        // ======================
        [HttpGet("guest-feed")]
        [AllowAnonymous] // tùy config auth; nếu không có global Authorize thì có thể bỏ
        public async Task<ActionResult<PagedResult<PostResponse>>> GetGuestFeed([FromQuery] int page = 1,[FromQuery] int pageSize = 20)
        {
            var result = await _posts.GetGuestFeedAsync(page, pageSize);
            return Ok(result);
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
                return BadRequest(new { code = "EMPTY_POST", message = "Bài viết trống. Cần có caption hoặc ít nhất 1 tag." });

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
            return ok
                ? Ok(new { message = "Đã cập nhật bài viết." })
                : StatusCode(403, new { code = "POST_FORBIDDEN_OR_NOT_FOUND", message = "Không thể chỉnh sửa bài viết (không tồn tại hoặc bạn không phải chủ sở hữu)." });
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
            return ok
                ? NoContent()
                : StatusCode(403, new { code = "POST_FORBIDDEN_OR_NOT_FOUND", message = "Không thể xoá bài viết (không tồn tại hoặc bạn không phải chủ sở hữu)." });
        }

        // ======================
        // 🔹 POST: Upload Media
        // ======================
        [Authorize]
        [HttpPost("{id:guid}/media")]
        public async Task<ActionResult<IEnumerable<PostMediaResponse>>> UploadMedia(Guid id, [FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest(new { code = "NO_FILE", message = "Không có file nào được gửi." });

            var requesterId = User.GetUserIdFromMetadata();

            // Vì service trả empty cho 'không phải owner' hoặc 'không có post',
            // controller nên phân biệt trước để trả message rõ ràng.
            var postEntity = await _posts.GetEntityAsync(id);
            if (postEntity is null)
                return NotFound(new { code = "POST_NOT_FOUND", message = "Bài viết không tồn tại." });

            if (postEntity.ProfileId != requesterId)
                return StatusCode(403, new { code = "NOT_OWNER", message = "Chỉ chủ bài viết mới được upload media." });

            var result = await _posts.UploadMediaAsync(id, files, requesterId);
            if (!result.Any())
                return BadRequest(new { code = "UPLOAD_FAILED", message = "Upload thất bại hoặc tất cả file bị bỏ qua." });

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

            if (!Enum.TryParse<ReactionType>(type, true, out var reactionType))
                return BadRequest(new { code = "INVALID_REACTION_TYPE", message = $"Giá trị reaction '{type}' không hợp lệ." });

            var postEntity = await _posts.GetEntityAsync(id);
            if (postEntity is null)
                return NotFound(new { code = "POST_NOT_FOUND", message = "Bài viết không tồn tại." });

            var newState = await _posts.ToggleReactionAsync(id, userId, reactionType);

            // Service trả null khi: 1) post không có (đã check ở trên) hoặc 2) reaction bị gỡ
            if (newState is null)
                return Ok(new { removed = true, message = "Reaction đã được gỡ." });

            return Ok(new { type = newState.ToString(), message = "Reaction đã được cập nhật." });
        }
    }
}

using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        private readonly ISearchService _search;
        public PostsController(IPostService posts, ITagService tags, IPrivacyGuard privacy, IProfileService profileService, ISearchService search)
        {
            _posts = posts;
            _tags = tags;
            _privacy = privacy;
            _profileService = profileService;
            _search = search;
        }

        // ======================
        // 🔹 Helper
        // ======================
        private Guid? TryGetUserId()
        {
            if (User?.Identity?.IsAuthenticated != true) return null;
            try { return User.GetUserId(); }
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
            var userId = User.GetUserId();
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
        // 🔹 GET: Feed cá nhân với Reposts (posts + reposts)
        // ======================
        [Authorize]
        [HttpGet("feed-with-reposts")]
        public async Task<ActionResult<PagedResult<FeedItemDto>>> GetFeedWithReposts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.GetUserId();
            var result = await _posts.GetFeedWithRepostsAsync(userId, page, pageSize);
            return Ok(result);
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
        // 🔹 GET: Explore 
        // ======================
        [Authorize]
        [HttpGet("explore")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetExplore([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            return Ok(await _posts.GetExploreAsync(User.GetUserId(), page, pageSize));
        }

        // ======================
        // 🔹 GET: Latest posts 
        // ======================
        [HttpGet("latest")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetLatest([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
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
        // 🔹 GET: Related posts (tags + semantic similarity)
        // ======================
        [HttpGet("{id:guid}/related")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetRelated(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = TryGetUserId();
            var post = await _posts.GetEntityAsync(id);
            if (post == null)
                return NotFound(new { code = "POST_NOT_FOUND", message = "Bài viết không tồn tại hoặc đã bị xoá." });

            var relatedPosts = new List<PostResponse>();

            // Strategy 1: Get posts with same tags
            var tagIds = post.PostTags?.Select(t => t.TagId).ToList() ?? new List<Guid>();

            if (tagIds.Any())
            {
                foreach (var tagId in tagIds)
                {
                    var tagPosts = await _tags.GetPostsByTagAsync(tagId, 1, pageSize * 2);
                    foreach (var p in tagPosts.Items.Where(p => p.Id != id))
                    {
                        if (!relatedPosts.Any(rp => rp.Id == p.Id))
                        {
                            var entity = await _posts.GetEntityAsync(p.Id);
                            if (await _privacy.CanViewPostAsync(entity, userId))
                            {
                                relatedPosts.Add(p);
                            }
                        }
                    }
                }
            }

            // Strategy 2: If not enough posts from tags, use semantic search with caption
            if (relatedPosts.Count < pageSize && !string.IsNullOrEmpty(post.Caption))
            {
                var semanticResult = await _search.SemanticSearchAsync(
                    new SemanticSearchRequest(post.Caption, 1, pageSize, 50),
                    userId ?? Guid.Empty);

                foreach (var searchPost in semanticResult.Posts.Where(p => p.Id != id))
                {
                    if (!relatedPosts.Any(rp => rp.Id == searchPost.Id))
                    {
                        var entity = await _posts.GetEntityAsync(searchPost.Id);
                        if (await _privacy.CanViewPostAsync(entity, userId))
                        {
                            var postResponse = await _posts.GetByIdAsync(searchPost.Id, userId);
                            if (postResponse != null)
                            {
                                relatedPosts.Add(postResponse);
                            }
                        }
                    }
                }
            }

            // Remove the original post
            relatedPosts.RemoveAll(p => p.Id == id);

            // Paginate results
            var paginated = relatedPosts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new PagedResult<PostResponse>(
                paginated,
                page,
                pageSize,
                relatedPosts.Count
            ));
        }

        // ======================
        // 🔹 POST: Tạo post
        // ======================
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<PostResponse>> Create([FromForm] CreatePostRequest dto, [FromForm] List<IFormFile>? mediaFiles)
        {
            var authorId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(dto.Caption) && (dto.Tags == null || !dto.Tags.Any()))
                return BadRequest(new { code = "EMPTY_POST", message = "Bài viết trống. Cần có caption hoặc ít nhất 1 tag." });

            try
            {
                // Use media files from the separate parameter, not from DTO
                var created = await _posts.CreateAsync(authorId, dto.Caption, dto.Tags, dto.PrivacyLevel, dto.Location, mediaFiles);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to upload media file"))
            {
                return BadRequest(new { 
                    code = "MEDIA_UPLOAD_FAILED", 
                    message = ex.Message,
                    details = "The post was not created due to media upload failure. Please check your media files and try again."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    code = "POST_CREATION_FAILED", 
                    message = "Failed to create post. Please try again later.",
                    details = ex.Message
                });
            }
        }

        // ======================
        // 🔹 PUT: Cập nhật caption
        // ======================
        [Authorize]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdatePostRequest dto)
        {
            var requesterId = User.GetUserId();
            var ok = await _posts.UpdateAsync(id, requesterId, dto.Caption);
            return ok
                ? Ok(new { message = "Đã cập nhật bài viết." })
                : StatusCode(403, new { code = "POST_FORBIDDEN_OR_NOT_FOUND", message = "Không thể chỉnh sửa bài viết (không tồn tại hoặc bạn không phải chủ sở hữu)." });
        }

        // ======================
        // 🔹 DELETE: Xoá post (soft delete - move to recycle bin)
        // ======================
        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var requesterId = User.GetUserId();
            var ok = await _posts.DeleteAsync(id, requesterId);
            return ok
                ? Ok(new { message = "Bài viết đã được chuyển vào thùng rác." })
                : StatusCode(403, new { code = "POST_FORBIDDEN_OR_NOT_FOUND", message = "Không thể xoá bài viết (không tồn tại hoặc bạn không phải chủ sở hữu)." });
        }

        // ======================
        // 🔹 POST: Restore post from recycle bin
        // ======================
        [Authorize]
        [HttpPost("{id:guid}/restore")]
        public async Task<IActionResult> Restore(Guid id)
        {
            var requesterId = User.GetUserId();
            var ok = await _posts.RestoreAsync(id, requesterId);
            return ok
                ? Ok(new { message = "Bài viết đã được khôi phục." })
                : StatusCode(403, new { code = "POST_RESTORE_FAILED", message = "Không thể khôi phục bài viết (không tồn tại, không bị xoá hoặc bạn không phải chủ sở hữu)." });
        }

        // ======================
        // 🔹 DELETE: Permanently delete post (hard delete)
        // ======================
        [Authorize]
        [HttpDelete("{id:guid}/permanent")]
        public async Task<IActionResult> PermanentDelete(Guid id)
        {
            var requesterId = User.GetUserId();
            var ok = await _posts.PermanentDeleteAsync(id, requesterId);
            return ok
                ? Ok(new { message = "Bài viết đã được xoá vĩnh viễn." })
                : StatusCode(403, new { code = "POST_PERMANENT_DELETE_FAILED", message = "Không thể xoá vĩnh viễn bài viết (không tồn tại hoặc bạn không phải chủ sở hữu)." });
        }

        // ======================
        // 🔹 GET: Recycle bin posts
        // ======================
        [Authorize]
        [HttpGet("recycle-bin")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetRecycleBin([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.GetUserId();
            var result = await _posts.GetRecycleBinAsync(userId, page, pageSize);
            return Ok(result);
        }

        // ======================
        // 🔹 POST: Archive post
        // ======================
        [Authorize]
        [HttpPost("{id:guid}/archive")]
        public async Task<IActionResult> Archive(Guid id)
        {
            var requesterId = User.GetUserId();
            var ok = await _posts.ArchiveAsync(id, requesterId);
            return ok
                ? Ok(new { message = "Bài viết đã được lưu trữ." })
                : StatusCode(403, new { code = "POST_ARCHIVE_FAILED", message = "Không thể lưu trữ bài viết (không tồn tại, đã bị xoá hoặc bạn không phải chủ sở hữu)." });
        }

        // ======================
        // 🔹 POST: Unarchive post
        // ======================
        [Authorize]
        [HttpPost("{id:guid}/unarchive")]
        public async Task<IActionResult> Unarchive(Guid id)
        {
            var requesterId = User.GetUserId();
            var ok = await _posts.UnarchiveAsync(id, requesterId);
            return ok
                ? Ok(new { message = "Bài viết đã được bỏ lưu trữ." })
                : StatusCode(403, new { code = "POST_UNARCHIVE_FAILED", message = "Không thể bỏ lưu trữ bài viết (không tồn tại hoặc bạn không phải chủ sở hữu)." });
        }

        // ======================
        // 🔹 GET: Archived posts
        // ======================
        [Authorize]
        [HttpGet("archived")]
        public async Task<ActionResult<PagedResult<PostResponse>>> GetArchived([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.GetUserId();
            var result = await _posts.GetArchivedAsync(userId, page, pageSize);
            return Ok(result);
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

            var requesterId = User.GetUserId();

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
            var userId = User.GetUserId();

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

        // ======================
        // 🔹 GET: Reactors (người đã react)
        // ======================
        [Authorize]
        [HttpGet("{id:guid}/reactors")]
        public async Task<ActionResult<IEnumerable<PostReactorResponse>>> GetReactors(Guid id)
        {
            var userId = User.GetUserId();

            try
            {
                var reactors = await _posts.GetReactorsAsync(id, userId);
                return Ok(reactors);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // ======================
        // 🔹 POST: Share/Repost post to profile
        // ======================
        [Authorize]
        [HttpPost("{id:guid}/share")]
        public async Task<ActionResult<RepostResponse>> SharePost(Guid id, [FromBody] CreateRepostRequest dto)
        {
            var userId = User.GetUserId();

            // Check if post exists
            var postEntity = await _posts.GetEntityAsync(id);
            if (postEntity is null)
                return NotFound(new { code = "POST_NOT_FOUND", message = "Bài viết không tồn tại." });

            var result = await _posts.SharePostAsync(id, userId, dto.Caption);

            if (result is null)
                return StatusCode(403, new { code = "SHARE_FORBIDDEN", message = "Không thể chia sẻ bài viết này." });

            return Ok(result);
        }

        // ======================
        // 🔹 DELETE: Unshare/Remove repost from profile
        // ======================
        [Authorize]
        [HttpDelete("{id:guid}/share")]
        public async Task<IActionResult> UnsharePost(Guid id)
        {
            var userId = User.GetUserId();
            var ok = await _posts.UnsharePostAsync(id, userId);
            return ok
                ? Ok(new { message = "Đã bỏ chia sẻ bài viết." })
                : NotFound(new { code = "SHARE_NOT_FOUND", message = "Bạn chưa chia sẻ bài viết này." });
        }

        // ======================
        // 🔹 GET: Get reposts by profile
        // ======================
        [HttpGet("profile/{profileId:guid}/shares")]
        public async Task<ActionResult<PagedResult<RepostResponse>>> GetProfileShares(Guid profileId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var currentUserId = TryGetUserId();
            var result = await _posts.GetRepostsByProfileAsync(profileId, currentUserId, page, pageSize);
            return Ok(result);
        }

        // ======================
        // 🔹 GET: Get repost by ID (for viewing shared posts)
        // ======================
        [HttpGet("shares/{repostId:guid}")]
        public async Task<ActionResult<RepostResponse>> GetRepost(Guid repostId)
        {
            var currentUserId = TryGetUserId();
            var result = await _posts.GetRepostAsync(repostId, currentUserId);

            if (result == null)
                return NotFound(new { code = "REPOST_NOT_FOUND", message = "Bài chia sẻ không tồn tại." });

            return Ok(result);
        }
    }
}

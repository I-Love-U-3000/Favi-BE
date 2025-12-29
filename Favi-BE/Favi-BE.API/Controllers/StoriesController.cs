using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoriesController : ControllerBase
    {
        private readonly IStoryService _stories;
        private readonly IPrivacyGuard _privacy;
        private readonly IProfileService _profileService;

        public StoriesController(IStoryService stories, IPrivacyGuard privacy, IProfileService profileService)
        {
            _stories = stories;
            _privacy = privacy;
            _profileService = profileService;
        }

        private Guid? TryGetUserId()
        {
            if (User?.Identity?.IsAuthenticated != true) return null;
            try { return User.GetUserIdFromMetadata(); }
            catch { return null; }
        }

        // GET: api/stories/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<StoryResponse>> GetById(Guid id)
        {
            var viewerId = TryGetUserId();
            var story = await _stories.GetByIdAsync(id, viewerId);

            if (story == null)
                return NotFound(new { code = "STORY_NOT_FOUND", message = "Story not found or expired." });

            return Ok(story);
        }

        // GET: api/stories/profile/{profileId}
        [HttpGet("profile/{profileId:guid}")]
        public async Task<ActionResult<IEnumerable<StoryResponse>>> GetByProfile(Guid profileId)
        {
            var viewerId = TryGetUserId();
            var profile = await _profileService.GetEntityByIdAsync(profileId);

            if (profile == null)
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Profile not found." });

            var stories = await _stories.GetActiveStoriesByProfileAsync(profileId, viewerId);
            return Ok(stories);
        }

        // GET: api/stories/feed (stories from following)
        [Authorize]
        [HttpGet("feed")]
        public async Task<ActionResult<IEnumerable<StoryFeedResponse>>> GetFeed()
        {
            var viewerId = User.GetUserIdFromMetadata();
            var stories = await _stories.GetViewableStoriesAsync(viewerId);

            // Group by profile for feed display
            var grouped = stories
                .GroupBy(s => new { s.ProfileId, s.ProfileUsername, s.ProfileAvatarUrl })
                .Select(g => new StoryFeedResponse(
                    g.Key.ProfileId,
                    g.Key.ProfileUsername,
                    g.Key.ProfileAvatarUrl,
                    g.ToList()
                ))
                .OrderByDescending(g => g.Stories.First().CreatedAt);

            return Ok(grouped);
        }

        // GET: api/stories/archived
        [Authorize]
        [HttpGet("archived")]
        public async Task<ActionResult<IEnumerable<StoryResponse>>> GetArchived()
        {
            var userId = User.GetUserIdFromMetadata();
            var stories = await _stories.GetArchivedStoriesAsync(userId);
            return Ok(stories);
        }

        // POST: api/stories
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<StoryResponse>> Create([FromForm] CreateStoryRequest request, [FromForm] IFormFile media)
        {
            var userId = User.GetUserIdFromMetadata();

            try
            {
                var created = await _stories.CreateAsync(userId, media, request.PrivacyLevel);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { code = "INVALID_MEDIA", message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = "STORY_CREATION_FAILED", message = "Failed to create story.", details = ex.Message });
            }
        }

        // POST: api/stories/{id}/archive
        [Authorize]
        [HttpPost("{id:guid}/archive")]
        public async Task<IActionResult> Archive(Guid id)
        {
            var userId = User.GetUserIdFromMetadata();
            var success = await _stories.ArchiveAsync(id, userId);

            return success
                ? Ok(new { message = "Story archived successfully." })
                : NotFound(new { code = "STORY_NOT_FOUND", message = "Story not found or you're not the owner." });
        }

        // DELETE: api/stories/{id}
        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserIdFromMetadata();
            var success = await _stories.DeleteAsync(id, userId);

            return success
                ? Ok(new { message = "Story deleted permanently." })
                : NotFound(new { code = "STORY_NOT_FOUND", message = "Story not found or you're not the owner." });
        }

        // POST: api/stories/{id}/view
        [Authorize]
        [HttpPost("{id:guid}/view")]
        public async Task<IActionResult> RecordView(Guid id)
        {
            var userId = User.GetUserIdFromMetadata();
            await _stories.RecordViewAsync(id, userId);
            return Ok(new { message = "View recorded." });
        }

        // GET: api/stories/{id}/viewers
        [Authorize]
        [HttpGet("{id:guid}/viewers")]
        public async Task<ActionResult<IEnumerable<StoryViewerResponse>>> GetViewers(Guid id)
        {
            var userId = User.GetUserIdFromMetadata();

            try
            {
                var viewers = await _stories.GetViewersAsync(id, userId);
                return Ok(viewers);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // GET: api/stories/profile/{profileId}/count
        [HttpGet("profile/{profileId:guid}/count")]
        public async Task<ActionResult<int>> GetActiveStoryCount(Guid profileId)
        {
            var count = await _stories.GetActiveStoryCountAsync(profileId);
            return Ok(count);
        }
    }
}

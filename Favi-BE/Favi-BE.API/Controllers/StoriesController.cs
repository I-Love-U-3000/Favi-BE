using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.Stories.Application.Commands.ArchiveStory;
using Favi_BE.Modules.Stories.Application.Commands.CreateStory;
using Favi_BE.Modules.Stories.Application.Commands.DeleteStory;
using Favi_BE.Modules.Stories.Application.Commands.RecordStoryView;
using Favi_BE.Modules.Stories.Application.Contracts.ReadModels;
using Favi_BE.Modules.Stories.Application.Exceptions;
using Favi_BE.Modules.Stories.Application.Queries.GetActiveStoriesByProfile;
using Favi_BE.Modules.Stories.Application.Queries.GetActiveStoryCount;
using Favi_BE.Modules.Stories.Application.Queries.GetArchivedStories;
using Favi_BE.Modules.Stories.Application.Queries.GetStoryById;
using Favi_BE.Modules.Stories.Application.Queries.GetStoryViewers;
using Favi_BE.Modules.Stories.Application.Queries.GetViewableStories;
using Favi_BE.Modules.Stories.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoriesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICloudinaryService _cloudinary;

        public StoriesController(IMediator mediator, ICloudinaryService cloudinary)
        {
            _mediator = mediator;
            _cloudinary = cloudinary;
        }

        private Guid? TryGetUserId()
        {
            if (User?.Identity?.IsAuthenticated != true) return null;
            try { return User.GetUserId(); }
            catch { return null; }
        }

        // GET: api/stories/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<StoryResponse>> GetById(Guid id)
        {
            var viewerId = TryGetUserId();
            var story = await _mediator.Send(new GetStoryByIdQuery(id, viewerId));

            if (story is null)
                return NotFound(new { code = "STORY_NOT_FOUND", message = "Story not found or expired." });

            return Ok(MapToResponse(story));
        }

        // GET: api/stories/profile/{profileId}
        [HttpGet("profile/{profileId:guid}")]
        public async Task<ActionResult<IEnumerable<StoryResponse>>> GetByProfile(Guid profileId)
        {
            var viewerId = TryGetUserId();
            try
            {
                var stories = await _mediator.Send(new GetActiveStoriesByProfileQuery(profileId, viewerId));
                return Ok(stories.Select(MapToResponse));
            }
            catch (ProfileNotFoundException)
            {
                return NotFound(new { code = "PROFILE_NOT_FOUND", message = "Profile not found." });
            }
        }

        // GET: api/stories/feed (stories from following)
        [Authorize]
        [HttpGet("feed")]
        public async Task<ActionResult<IEnumerable<StoryFeedResponse>>> GetFeed()
        {
            var viewerId = User.GetUserId();
            var stories = await _mediator.Send(new GetViewableStoriesQuery(viewerId));

            var grouped = stories
                .GroupBy(s => new { s.ProfileId, s.ProfileUsername, s.ProfileAvatarUrl })
                .Select(g => new StoryFeedResponse(
                    g.Key.ProfileId,
                    g.Key.ProfileUsername,
                    g.Key.ProfileAvatarUrl,
                    g.Select(MapToResponse).ToList()
                ))
                .OrderByDescending(g => g.Stories.First().CreatedAt);

            return Ok(grouped);
        }

        // GET: api/stories/archived
        [Authorize]
        [HttpGet("archived")]
        public async Task<ActionResult<IEnumerable<StoryResponse>>> GetArchived()
        {
            var userId = User.GetUserId();
            var stories = await _mediator.Send(new GetArchivedStoriesQuery(userId));
            return Ok(stories.Select(MapToResponse));
        }

        // POST: api/stories
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<StoryResponse>> Create([FromForm] CreateStoryRequest request, [FromForm] IFormFile media)
        {
            var userId = User.GetUserId();

            if (media == null || media.Length == 0)
                return BadRequest(new { code = "INVALID_MEDIA", message = "Media file is required." });

            if (!media.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { code = "INVALID_MEDIA", message = "Only image files are supported for stories." });

            try
            {
                var uploaded = await _cloudinary.UploadAsyncOrThrow(media, folder: "favi_stories");

                var result = await _mediator.Send(new CreateStoryCommand(
                    AuthorId: userId,
                    MediaUrl: uploaded.Url,
                    MediaPublicId: uploaded.PublicId,
                    MediaWidth: uploaded.Width,
                    MediaHeight: uploaded.Height,
                    MediaFormat: uploaded.Format,
                    ThumbnailUrl: uploaded.ThumbnailUrl,
                    Privacy: MapPrivacy(request.PrivacyLevel)
                ));

                var created = await _mediator.Send(new GetStoryByIdQuery(result.StoryId, userId));
                if (created is null)
                    return StatusCode(500, new { code = "STORY_CREATION_FAILED", message = "Failed to reload created story." });

                return CreatedAtAction(nameof(GetById), new { id = result.StoryId }, MapToResponse(created));
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
            var userId = User.GetUserId();
            var result = await _mediator.Send(new ArchiveStoryCommand(id, userId));

            return result.Success
                ? Ok(new { message = "Story archived successfully." })
                : NotFound(new { code = result.ErrorCode, message = "Story not found or you're not the owner." });
        }

        // DELETE: api/stories/{id}
        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _mediator.Send(new DeleteStoryCommand(id, userId));

            if (!result.Success)
                return NotFound(new { code = result.ErrorCode, message = "Story not found or you're not the owner." });

            if (!string.IsNullOrWhiteSpace(result.MediaPublicId))
                await _cloudinary.TryDeleteAsync(result.MediaPublicId);

            return Ok(new { message = "Story deleted permanently." });
        }

        // POST: api/stories/{id}/view
        [Authorize]
        [HttpPost("{id:guid}/view")]
        public async Task<IActionResult> RecordView(Guid id)
        {
            var userId = User.GetUserId();
            await _mediator.Send(new RecordStoryViewCommand(id, userId));
            return Ok(new { message = "View recorded." });
        }

        // GET: api/stories/{id}/viewers
        [Authorize]
        [HttpGet("{id:guid}/viewers")]
        public async Task<ActionResult<IEnumerable<StoryViewerResponse>>> GetViewers(Guid id)
        {
            var userId = User.GetUserId();
            try
            {
                var viewers = await _mediator.Send(new GetStoryViewersQuery(id, userId));
                return Ok(viewers.Select(v => new StoryViewerResponse(
                    v.ViewerId, v.Username, v.DisplayName, v.AvatarUrl, v.ViewedAt)));
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
            var count = await _mediator.Send(new GetActiveStoryCountQuery(profileId));
            return Ok(count);
        }

        private static StoryPrivacy MapPrivacy(PrivacyLevel p) => (StoryPrivacy)(int)p;

        private static StoryResponse MapToResponse(StoryReadModel m) => new(
            m.Id,
            m.ProfileId,
            m.ProfileUsername,
            m.ProfileAvatarUrl,
            m.MediaUrl,
            m.ThumbnailUrl,
            m.CreatedAt,
            m.ExpiresAt,
            (PrivacyLevel)(int)m.Privacy,
            m.IsArchived,
            m.IsNSFW,
            m.ViewCount,
            m.HasViewed);
    }
}

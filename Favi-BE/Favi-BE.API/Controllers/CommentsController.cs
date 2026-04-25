using Favi_BE.API.Models.Dtos;
using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.Engagement.Application.Commands.CreateComment;
using Favi_BE.Modules.Engagement.Application.Commands.DeleteComment;
using Favi_BE.Modules.Engagement.Application.Commands.ToggleCommentReaction;
using Favi_BE.Modules.Engagement.Application.Commands.UpdateComment;
using Favi_BE.Modules.Engagement.Application.Contracts.ReadModels;
using Favi_BE.Modules.Engagement.Application.Queries.GetCommentById;
using Favi_BE.Modules.Engagement.Application.Queries.GetCommentReactors;
using Favi_BE.Modules.Engagement.Application.Queries.GetCommentsByPost;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EngagementReactionType = Favi_BE.Modules.Engagement.Domain.ReactionType;
using LegacyReactionType = Favi_BE.Models.Enums.ReactionType;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IPostService _posts;
        private readonly IPrivacyGuard _privacy;
        private readonly ICloudinaryService _cloudinary;

        public CommentsController(IMediator mediator, IPostService posts, IPrivacyGuard privacy, ICloudinaryService cloudinary)
        {
            _mediator = mediator;
            _posts = posts;
            _privacy = privacy;
            _cloudinary = cloudinary;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CommentResponse>> Create(CreateCommentRequest dto)
        {
            var userId = User.GetUserId();
            var result = await _mediator.Send(new CreateCommentCommand(
                PostId: dto.PostId,
                RepostId: null,
                AuthorId: userId,
                Content: dto.Content,
                MediaUrl: dto.MediaUrl,
                ParentCommentId: dto.ParentCommentId));

            if (!result.IsSuccess)
                return result.ErrorCode == "POST_NOT_FOUND"
                    ? NotFound(new { code = result.ErrorCode, message = result.ErrorMessage })
                    : BadRequest(new { code = result.ErrorCode, message = result.ErrorMessage });

            return Ok(MapToCommentResponse(result.Comment!));
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateCommentRequest dto)
        {
            var userId = User.GetUserId();
            var result = await _mediator.Send(new UpdateCommentCommand(id, userId, dto.Content));

            if (!result.IsSuccess)
                return NotFound(new { code = "COMMENT_NOT_FOUND_OR_FORBIDDEN", message = result.ErrorMessage });

            return Ok(MapToCommentResponse(result.Comment!));
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserId();
            var result = await _mediator.Send(new DeleteCommentCommand(id, userId));

            return result.IsSuccess
                ? Ok(new { message = "Đã xoá bình luận." })
                : NotFound(new { code = "COMMENT_NOT_FOUND_OR_FORBIDDEN", message = result.ErrorMessage });
        }

        [HttpGet("post/{postId}")]
        public async Task<ActionResult<PagedResult<CommentResponse>>> GetByPost(Guid postId, int page = 1, int pageSize = 20)
        {
            var viewerId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
            var post = await _posts.GetEntityAsync(postId);

            if (post == null)
                return NotFound(new { code = "POST_NOT_FOUND", message = "Bài viết không tồn tại." });

            if (!await _privacy.CanViewPostAsync(post, viewerId))
                return StatusCode(403, new { code = "POST_FORBIDDEN", message = "Bạn không có quyền xem bình luận của bài viết này." });

            var (items, total) = await _mediator.Send(new GetCommentsByPostQuery(postId, viewerId, page, pageSize));
            var dtos = items.Select(MapToCommentResponse).ToList();
            return Ok(new PagedResult<CommentResponse>(dtos, page, pageSize, total));
        }

        [Authorize]
        [HttpPost("{id:guid}/reactions")]
        public async Task<ActionResult> ToggleReaction(Guid id, [FromQuery] string type)
        {
            var userId = User.GetUserId();

            if (!Enum.TryParse<LegacyReactionType>(type, true, out var legacyType))
                return BadRequest(new { code = "INVALID_REACTION_TYPE", message = $"Giá trị reaction '{type}' không hợp lệ." });

            var result = await _mediator.Send(new ToggleCommentReactionCommand(
                id, userId, (EngagementReactionType)(int)legacyType));

            if (!result.IsSuccess)
                return NotFound(new { code = result.ErrorCode, message = result.ErrorMessage });

            if (result.Removed)
                return Ok(new { removed = true, message = "Reaction đã được gỡ." });

            return Ok(new { type = result.Type!.ToString(), message = "Reaction đã được cập nhật." });
        }

        [Authorize]
        [HttpGet("{id:guid}/reactors")]
        public async Task<ActionResult<IEnumerable<CommentReactorResponse>>> GetReactors(Guid id)
        {
            var comment = await _mediator.Send(new GetCommentByIdQuery(id, null));
            if (comment is null)
                return NotFound(new { code = "COMMENT_NOT_FOUND", message = "Bình luận không tồn tại." });

            var reactors = await _mediator.Send(new GetCommentReactorsQuery(id));
            return Ok(reactors.Select(r => new CommentReactorResponse(
                r.ProfileId,
                r.Username,
                r.DisplayName,
                r.AvatarUrl,
                (LegacyReactionType)(int)r.ReactionType,
                r.ReactedAt)));
        }

        [Authorize]
        [HttpPost("upload-image")]
        public async Task<ActionResult<ChatImageUploadResponse>> UploadImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            if (!file.ContentType.StartsWith("image/"))
                return BadRequest(new { message = "Only image files are allowed" });

            const long maxFileSize = 10 * 1024 * 1024;
            if (file.Length > maxFileSize)
                return BadRequest(new { message = "File size must be less than 10MB" });

            var uploadResult = await _cloudinary.TryUploadAsync(file, CancellationToken.None, "favi_comments");

            if (uploadResult == null)
                return StatusCode(500, new { message = "Failed to upload image" });

            return Ok(new ChatImageUploadResponse(
                uploadResult.Url,
                uploadResult.PublicId,
                uploadResult.Width,
                uploadResult.Height,
                uploadResult.Format
            ));
        }

        // -------------------------------------------------------------------------
        // Mapping helper — preserves legacy JSON contract
        // -------------------------------------------------------------------------

        private static CommentResponse MapToCommentResponse(CommentQueryDto dto)
        {
            var legacyByType = dto.Reactions.ByType
                .ToDictionary(kvp => (LegacyReactionType)(int)kvp.Key, kvp => kvp.Value);
            LegacyReactionType? legacyMine = dto.Reactions.CurrentUserReaction is { } t
                ? (LegacyReactionType)(int)t
                : null;
            var summary = new ReactionSummaryDto(dto.Reactions.Total, legacyByType, legacyMine);

            return new CommentResponse(
                dto.Id,
                dto.PostId,
                dto.ProfileId,
                dto.Content,
                dto.MediaUrl,
                dto.CreatedAt,
                dto.UpdatedAt,
                dto.ParentCommentId,
                summary);
        }
    }
}

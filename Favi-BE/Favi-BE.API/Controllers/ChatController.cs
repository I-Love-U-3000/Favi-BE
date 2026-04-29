using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Favi_BE.API.Models.Dtos;
using Favi_BE.Modules.Auth.Application.Commands.UpdateLastActive;
using Favi_BE.Modules.Messaging.Application.Commands.CreateGroupConversation;
using Favi_BE.Modules.Messaging.Application.Commands.GetOrCreateDm;
using Favi_BE.Modules.Messaging.Application.Commands.MarkConversationRead;
using Favi_BE.Modules.Messaging.Application.Commands.SendMessage;
using Favi_BE.Modules.Messaging.Application.Queries.GetConversations;
using Favi_BE.Modules.Messaging.Application.Queries.GetMessages;
using ConversationSummaryRM = Favi_BE.Modules.Messaging.Application.Contracts.ReadModels.ConversationSummaryReadModel;
using MessageRM = Favi_BE.Modules.Messaging.Application.Contracts.ReadModels.MessageReadModel;
using LegacyConvType = Favi_BE.API.Models.Enums.ConversationType;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICloudinaryService _cloudinary;

        public ChatController(IMediator mediator, ICloudinaryService cloudinary)
        {
            _mediator = mediator;
            _cloudinary = cloudinary;
        }

        [HttpPost("dm")]
        public async Task<ActionResult<ConversationSummaryDto>> GetOrCreateDm([FromBody] CreateDmConversationRequest dto)
        {
            var userId = User.GetUserId();
            await _mediator.Send(new UpdateLastActiveCommand(userId));
            var result = await _mediator.Send(new GetOrCreateDmCommand(userId, dto.OtherProfileId));
            if (result is null) return NotFound(new { code = "PROFILE_NOT_FOUND" });
            return Ok(MapConversation(result));
        }

        [HttpPost("group")]
        public async Task<ActionResult<ConversationSummaryDto>> CreateGroup([FromBody] CreateGroupConversationRequest dto)
        {
            var userId = User.GetUserId();
            await _mediator.Send(new UpdateLastActiveCommand(userId));
            var result = await _mediator.Send(new CreateGroupConversationCommand(userId, dto.MemberIds.ToList()));
            if (result is null) return BadRequest(new { code = "INVALID_MEMBERS" });
            return Ok(MapConversation(result));
        }

        [HttpGet("conversations")]
        public async Task<ActionResult<IEnumerable<ConversationSummaryDto>>> GetConversations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = User.GetUserId();
            await _mediator.Send(new UpdateLastActiveCommand(userId));
            var result = await _mediator.Send(new GetConversationsQuery(userId, page, pageSize));
            return Ok(result.Select(MapConversation));
        }

        [HttpGet("{conversationId:guid}/messages")]
        public async Task<ActionResult<object>> GetMessages(
            Guid conversationId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var userId = User.GetUserId();
            await _mediator.Send(new UpdateLastActiveCommand(userId));
            var (items, total) = await _mediator.Send(new GetMessagesQuery(conversationId, userId, page, pageSize));
            return Ok(new { items = items.Select(MapMessage), total, page, pageSize });
        }

        [HttpPost("{conversationId:guid}/messages")]
        public async Task<ActionResult<MessageDto>> SendMessage(
            Guid conversationId,
            [FromBody] SendMessageRequest dto)
        {
            var userId = User.GetUserId();
            await _mediator.Send(new UpdateLastActiveCommand(userId));
            var result = await _mediator.Send(new SendMessageCommand(userId, conversationId, dto.Content, dto.MediaUrl, dto.PostId));
            if (result is null) return BadRequest(new { code = "SEND_FAILED" });
            return Ok(MapMessage(result));
        }

        [HttpPost("{conversationId:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid conversationId, [FromBody] Guid lastMessageId)
        {
            var userId = User.GetUserId();
            await _mediator.Send(new UpdateLastActiveCommand(userId));
            var result = await _mediator.Send(new MarkConversationReadCommand(userId, conversationId, lastMessageId));
            if (!result.Succeeded) return Forbid();
            return NoContent();
        }

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

            var uploadResult = await _cloudinary.TryUploadAsync(file, CancellationToken.None, "favi_chat");
            if (uploadResult == null)
                return StatusCode(500, new { message = "Failed to upload image" });

            return Ok(new ChatImageUploadResponse(
                uploadResult.Url, uploadResult.PublicId,
                uploadResult.Width, uploadResult.Height, uploadResult.Format));
        }

        // ── Mapping helpers ────────────────────────────────────────────────────

        private static ConversationSummaryDto MapConversation(ConversationSummaryRM m) =>
            new(m.Id,
                (LegacyConvType)(int)m.Type,
                m.LastMessageAt,
                m.LastMessagePreview,
                m.UnreadCount,
                m.Members.Select(mb => new ConversationMemberDto(
                    mb.ProfileId, mb.Username, mb.DisplayName, mb.AvatarUrl, mb.LastActiveAt)));

        private static MessageDto MapMessage(MessageRM m) =>
            new(m.Id, m.ConversationId, m.SenderId,
                m.Username, m.DisplayName, m.AvatarUrl,
                m.Content, m.MediaUrl,
                m.CreatedAt, m.UpdatedAt, m.IsEdited,
                m.ReadBy,
                m.PostPreview is null ? null : new PostPreviewDto(
                    m.PostPreview.Id, m.PostPreview.AuthorProfileId, m.PostPreview.Caption,
                    m.PostPreview.ThumbnailUrl, m.PostPreview.MediasCount, m.PostPreview.CreatedAt));
    }
}

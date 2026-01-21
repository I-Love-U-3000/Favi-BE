using Favi_BE.API.Interfaces.Services;
using Favi_BE.API.Models.Dtos;
using Favi_BE.Common;
using Favi_BE.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chat;
        private readonly ICloudinaryService _cloudinary;

        public ChatController(IChatService chat, ICloudinaryService cloudinary)
        {
            _chat = chat;
            _cloudinary = cloudinary;
        }

        [HttpPost("dm")]
        public async Task<ActionResult<ConversationSummaryDto>> GetOrCreateDm([FromBody] CreateDmConversationRequest dto)
        {
            var currentUserId = User.GetUserId();
            var result = await _chat.GetOrCreateDmAsync(currentUserId, dto.OtherProfileId);
            return Ok(result);
        }

        [HttpPost("group")]
        public async Task<ActionResult<ConversationSummaryDto>> CreateGroup([FromBody] CreateGroupConversationRequest dto)
        {
            var currentUserId = User.GetUserId();
            var result = await _chat.CreateGroupAsync(currentUserId, dto);
            return Ok(result);
        }

        [HttpGet("conversations")]
        public async Task<ActionResult<IEnumerable<ConversationSummaryDto>>> GetConversations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUserId = User.GetUserId();
                // Update user's last active time when they fetch conversations
                await _chat.UpdateUserLastActiveAsync(currentUserId);
                var result = await _chat.GetConversationsAsync(currentUserId, page, pageSize);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception here for debugging
                return StatusCode(500, "An error occurred while retrieving conversations");
            }
        }

        [HttpGet("{conversationId:guid}/messages")]
        public async Task<ActionResult<object>> GetMessages(
            Guid conversationId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var currentUserId = User.GetUserId();
            // Update user's last active time when they fetch messages
            await _chat.UpdateUserLastActiveAsync(currentUserId);
            var (items, total) = await _chat.GetMessagesAsync(currentUserId, conversationId, page, pageSize);

            return Ok(new
            {
                items,
                total,
                page,
                pageSize
            });
        }

        [HttpPost("{conversationId:guid}/messages")]
        public async Task<ActionResult<MessageDto>> SendMessage(
            Guid conversationId,
            [FromBody] SendMessageRequest dto)
        {
            var currentUserId = User.GetUserId();
            // Update user's last active time when they send a message
            await _chat.UpdateUserLastActiveAsync(currentUserId);
            var result = await _chat.SendMessageAsync(currentUserId, conversationId, dto);
            return Ok(result);
        }

        [HttpPost("{conversationId:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid conversationId, [FromBody] Guid lastMessageId)
        {
            var currentUserId = User.GetUserId();
            // Update user's last active time when they mark messages as read
            await _chat.UpdateUserLastActiveAsync(currentUserId);
            await _chat.MarkAsReadAsync(currentUserId, conversationId, lastMessageId);
            return NoContent();
        }

        [HttpPost("upload-image")]
        public async Task<ActionResult<ChatImageUploadResponse>> UploadImage([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            // Validate file type (only images)
            if (!file.ContentType.StartsWith("image/"))
                return BadRequest(new { message = "Only image files are allowed" });

            // Validate file size (max 10MB)
            const long maxFileSize = 10 * 1024 * 1024;
            if (file.Length > maxFileSize)
                return BadRequest(new { message = "File size must be less than 10MB" });

            var uploadResult = await _cloudinary.TryUploadAsync(file, CancellationToken.None, "favi_chat");

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
    }
}

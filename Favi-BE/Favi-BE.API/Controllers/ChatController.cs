using Favi_BE.API.Interfaces.Services;
using Favi_BE.API.Models.Dtos;
using Favi_BE.Common;
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

        public ChatController(IChatService chat)
        {
            _chat = chat;
        }

        [HttpPost("dm")]
        public async Task<ActionResult<ConversationSummaryDto>> GetOrCreateDm([FromBody] CreateDmConversationRequest dto)
        {
            var currentUserId = User.GetUserIdFromMetadata();
            var result = await _chat.GetOrCreateDmAsync(currentUserId, dto.OtherProfileId);
            return Ok(result);
        }

        [HttpPost("group")]
        public async Task<ActionResult<ConversationSummaryDto>> CreateGroup([FromBody] CreateGroupConversationRequest dto)
        {
            var currentUserId = User.GetUserIdFromMetadata();
            var result = await _chat.CreateGroupAsync(currentUserId, dto);
            return Ok(result);
        }

        [HttpGet("conversations")]
        public async Task<ActionResult<IEnumerable<ConversationSummaryDto>>> GetConversations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var currentUserId = User.GetUserIdFromMetadata();
            var result = await _chat.GetConversationsAsync(currentUserId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{conversationId:guid}/messages")]
        public async Task<ActionResult<object>> GetMessages(
            Guid conversationId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var currentUserId = User.GetUserIdFromMetadata();
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
            var currentUserId = User.GetUserIdFromMetadata();
            var result = await _chat.SendMessageAsync(currentUserId, conversationId, dto);
            return Ok(result);
        }

        [HttpPost("{conversationId:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid conversationId, [FromBody] Guid lastMessageId)
        {
            var currentUserId = User.GetUserIdFromMetadata();
            await _chat.MarkAsReadAsync(currentUserId, conversationId, lastMessageId);
            return NoContent();
        }
    }
}

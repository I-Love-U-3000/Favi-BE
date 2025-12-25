using Favi_BE.API.Interfaces.Repositories;
using Favi_BE.API.Interfaces.Services;
using Favi_BE.API.Models.Dtos;
using Favi_BE.API.Models.Entities;
using Favi_BE.API.Models.Entities.JoinTables;
using Favi_BE.API.Models.Enums;
using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Favi_BE.API.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _uow;
        private readonly IConversationRepository _conversations;
        private readonly IMessageRepository _messages;
        private readonly IUserConversationRepository _userConversations;
        private readonly IProfileRepository _profiles;
        private readonly ILogger<ChatService> _logger;

        public ChatService(IUnitOfWork uow, ILogger<ChatService> logger)
        {
            _uow = uow;
            _conversations = uow.Conversations;
            _messages = uow.Messages;
            _userConversations = uow.UserConversations;
            _profiles = uow.Profiles;
            _logger = logger;
        }

        public async Task UpdateUserLastActiveAsync(Guid userId)
        {
            try
            {
                var profile = await _profiles.GetByIdAsync(userId);
                if (profile != null)
                {
                    profile.LastActiveAt = DateTime.UtcNow;
                    _profiles.Update(profile);
                    await _uow.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last active time for user {UserId}", userId);
            }
        }

        public async Task<ConversationSummaryDto> GetOrCreateDmAsync(Guid currentProfileId, Guid otherProfileId)
        {
            try
            {
                // Check if other profile exists
                var otherProfile = await _profiles.GetByIdAsync(otherProfileId);
                if (otherProfile == null)
                    throw new ArgumentException("Other user not found");

                // Find existing DM conversation
                var existingConv = await _conversations.FindDmConversationAsync(currentProfileId, otherProfileId);
                
                if (existingConv != null)
                {
                    return await MapToSummaryDtoAsync(existingConv, currentProfileId);
                }

                // Create new DM conversation
                var conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    Type = ConversationType.Dm,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = null
                };

                await _conversations.AddAsync(conversation);

                // Add both users to conversation
                var userConvs = new List<UserConversation>
                {
                    new UserConversation
                    {
                        ConversationId = conversation.Id,
                        ProfileId = currentProfileId,
                        Role = "member",
                        JoinedAt = DateTime.UtcNow
                    },
                    new UserConversation
                    {
                        ConversationId = conversation.Id,
                        ProfileId = otherProfileId,
                        Role = "member",
                        JoinedAt = DateTime.UtcNow
                    }
                };

                await _userConversations.AddRangeAsync(userConvs);
                await _uow.CompleteAsync();

                // Load full conversation for mapping
                var fullConv = await _conversations.GetConversationWithMembersAsync(conversation.Id)
                              ?? conversation;
                
                return await MapToSummaryDtoAsync(fullConv, currentProfileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating DM between {User1} and {User2}", currentProfileId, otherProfileId);
                throw;
            }
        }

        public async Task<ConversationSummaryDto> CreateGroupAsync(Guid currentProfileId, CreateGroupConversationRequest dto)
        {
            try
            {
                if (dto.MemberIds == null || !dto.MemberIds.Any())
                    throw new ArgumentException("At least one member is required");

                // Validate all members exist
                var memberIds = dto.MemberIds.Distinct().ToList();
                if (memberIds.Contains(currentProfileId))
                    throw new ArgumentException("Cannot include yourself in group creation");

                var members = await _profiles.FindAsync(p => memberIds.Contains(p.Id));
                if (members.Count() != memberIds.Count)
                    throw new ArgumentException("One or more members not found");

                // Create group conversation
                var conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    Type = ConversationType.Group,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = null
                };

                await _conversations.AddAsync(conversation);

                // Add creator as owner and members
                var userConvs = new List<UserConversation>
                {
                    new UserConversation
                    {
                        ConversationId = conversation.Id,
                        ProfileId = currentProfileId,
                        Role = "owner",
                        JoinedAt = DateTime.UtcNow
                    }
                };

                foreach (var memberId in memberIds)
                {
                    userConvs.Add(new UserConversation
                    {
                        ConversationId = conversation.Id,
                        ProfileId = memberId,
                        Role = "member",
                        JoinedAt = DateTime.UtcNow
                    });
                }

                await _userConversations.AddRangeAsync(userConvs);
                await _uow.CompleteAsync();

                // Load full conversation for mapping
                var fullConv = await _conversations.GetConversationWithMembersAsync(conversation.Id)
                              ?? conversation;
                
                return await MapToSummaryDtoAsync(fullConv, currentProfileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group chat for user {UserId}", currentProfileId);
                throw;
            }
        }

        public async Task<IEnumerable<ConversationSummaryDto>> GetConversationsAsync(Guid currentProfileId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var skip = (page - 1) * pageSize;
            var conversations = await _conversations.GetConversationsForUserAsync(currentProfileId, skip, pageSize);

            var summaries = new List<ConversationSummaryDto>();
            foreach (var conv in conversations)
            {
                var summary = await MapToSummaryDtoAsync(conv, currentProfileId);
                summaries.Add(summary);
            }

            return summaries;
        }

        public async Task<(IEnumerable<MessageDto> Items, int Total)> GetMessagesAsync(
            Guid currentProfileId, Guid conversationId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            // Verify user has access to conversation
            var userConv = await _userConversations.GetAsync(conversationId, currentProfileId);
            if (userConv == null)
                throw new UnauthorizedAccessException("Access denied to conversation");

            var skip = (page - 1) * pageSize;
            var (messages, total) = await _messages.GetMessagesForConversationAsync(conversationId, skip, pageSize);

            var messageDtos = messages.Select(m => MapToMessageDto(m)).ToList();
            
            // Update last read message
            if (messages.Any())
            {
                try
                {
                    var lastMessage = messages.First();
                    userConv.LastReadMessageId = lastMessage.Id;
                    _userConversations.Update(userConv);
                    await _uow.CompleteAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating last read message for user {UserId} in conversation {ConvId}", currentProfileId, conversationId);
                    // Non-critical, swallow exception
                }
            }

            return (messageDtos, total);
        }

        public async Task<MessageDto> SendMessageAsync(
            Guid currentProfileId, Guid conversationId, SendMessageRequest dto)
        {
            try
            {
                // Verify user has access to conversation
                var userConv = await _userConversations.GetAsync(conversationId, currentProfileId);
                if (userConv == null)
                    throw new UnauthorizedAccessException("Access denied to conversation");

                if (string.IsNullOrWhiteSpace(dto.Content) && string.IsNullOrWhiteSpace(dto.MediaUrl))
                    throw new ArgumentException("Message must have content or media");

                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversationId,
                    SenderId = currentProfileId,
                    Content = dto.Content?.Trim(),
                    MediaUrl = dto.MediaUrl?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null,
                    IsEdited = false
                };

                await _messages.AddAsync(message);

                // Update conversation last message time
                var conversation = await _conversations.GetByIdAsync(conversationId);
                if (conversation != null)
                {
                    conversation.LastMessageAt = message.CreatedAt;
                    _conversations.Update(conversation);
                }

                await _uow.CompleteAsync();

                return MapToMessageDto(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message in conversation {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task MarkAsReadAsync(Guid currentProfileId, Guid conversationId, Guid lastMessageId)
        {
            try
            {
                // Verify user has access to conversation
                var userConv = await _userConversations.GetAsync(conversationId, currentProfileId);
                if (userConv == null)
                    throw new UnauthorizedAccessException("Access denied to conversation");

                // Verify message exists and belongs to conversation
                var message = await _messages.GetByIdAsync(lastMessageId);
                if (message == null || message.ConversationId != conversationId)
                    throw new ArgumentException("Invalid message");

                userConv.LastReadMessageId = lastMessageId;
                _userConversations.Update(userConv);
                await _uow.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking conversation {ConversationId} as read", conversationId);
                throw;
            }
        }

        private async Task<ConversationSummaryDto> MapToSummaryDtoAsync(Conversation conversation, Guid currentProfileId)
        {
            // Load related data if not already loaded
            if (conversation.UserConversations == null || !conversation.UserConversations.Any())
            {
                conversation = await _conversations.GetConversationWithMembersAsync(conversation.Id)
                              ?? conversation;
            }

            var members = conversation.UserConversations?
                .Select(uc => new ConversationMemberDto(
                    uc.ProfileId,
                    uc.Profile?.Username ?? "Unknown",
                    uc.Profile?.DisplayName,
                    uc.Profile?.AvatarUrl
                )) ?? new List<ConversationMemberDto>();

            // Get last message for preview
            var lastMessage = await _messages.GetLastMessageAsync(conversation.Id);
            string? lastMessagePreview = null;
            if (lastMessage != null)
            {
                lastMessagePreview = !string.IsNullOrWhiteSpace(lastMessage.Content)
                    ? (lastMessage.Content.Length > 50 
                        ? lastMessage.Content.Substring(0, 47) + "..."
                        : lastMessage.Content)
                    : "[Media]";
            }

            // Get unread count
            var userConv = conversation.UserConversations?
                .FirstOrDefault(uc => uc.ProfileId == currentProfileId);

            var unreadCount = 0;
            if (userConv?.LastReadMessageId != null)
            {
                DateTime lastReadTime;
                if (userConv.LastReadMessage != null)
                {
                    lastReadTime = userConv.LastReadMessage.CreatedAt;
                }
                else
                {
                    // If LastReadMessageId exists but LastReadMessage is null, fetch the message
                    var lastReadMessage = await _messages.GetByIdAsync(userConv.LastReadMessageId.Value);
                    lastReadTime = lastReadMessage?.CreatedAt ?? DateTime.MinValue;
                }

                unreadCount = await _messages.GetUnreadCountAsync(conversation.Id, lastReadTime);
            }
            else
            {
                // If no last read message, count all messages as unread
                var allMessages = await _messages.FindAsync(m => m.ConversationId == conversation.Id);
                unreadCount = allMessages.Count();
            }

            return new ConversationSummaryDto(
                conversation.Id,
                conversation.Type,
                conversation.LastMessageAt,
                lastMessagePreview,
                unreadCount,
                members
            );
        }

        private MessageDto MapToMessageDto(Message message)
        {
            return new MessageDto(
                message.Id,
                message.ConversationId,
                message.SenderId,
                message.Sender?.Username ?? "Unknown",
                message.Sender?.DisplayName,
                message.Sender?.AvatarUrl,
                message.Content,
                message.MediaUrl,
                message.CreatedAt,
                message.UpdatedAt,
                message.IsEdited
            );
        }
    }
}

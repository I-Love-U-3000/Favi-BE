using Favi_BE.API.Interfaces.Services;
using Favi_BE.API.Models.Dtos;
using Favi_BE.API.Models.Entities;
using Favi_BE.API.Models.Entities.JoinTables;
using Favi_BE.API.Models.Enums;
using Favi_BE.Interfaces;

namespace Favi_BE.API.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _uow;

        public ChatService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ConversationSummaryDto> GetOrCreateDmAsync(Guid currentProfileId, Guid otherProfileId)
        {
            if (currentProfileId == otherProfileId)
                throw new ArgumentException("Cannot create DM with yourself.");

            var existing = await _uow.Conversations.FindDmConversationAsync(currentProfileId, otherProfileId);
            if (existing is null)
            {
                var now = DateTime.UtcNow;

                var conversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    Type = ConversationType.Dm,
                    CreatedAt = now
                };

                await _uow.Conversations.AddAsync(conversation);

                var memberCurrent = new UserConversation
                {
                    ConversationId = conversation.Id,
                    ProfileId = currentProfileId,
                    Role = "member",
                    JoinedAt = now
                };
                var memberOther = new UserConversation
                {
                    ConversationId = conversation.Id,
                    ProfileId = otherProfileId,
                    Role = "member",
                    JoinedAt = now
                };

                await _uow.UserConversations.AddAsync(memberCurrent);
                await _uow.UserConversations.AddAsync(memberOther);

                await _uow.CompleteAsync();

                existing = conversation;
            }

            var convoWithMembers = await _uow.Conversations.GetConversationWithMembersAsync(existing.Id)
                                  ?? existing;

            return await MapConversationSummaryAsync(convoWithMembers, currentProfileId);
        }

        public async Task<ConversationSummaryDto> CreateGroupAsync(Guid currentProfileId, CreateGroupConversationRequest dto)
        {
            var members = dto.MemberIds.Distinct().ToList();
            if (!members.Contains(currentProfileId))
                members.Add(currentProfileId);

            var now = DateTime.UtcNow;

            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                Type = ConversationType.Group,
                CreatedAt = now
            };

            await _uow.Conversations.AddAsync(conversation);

            foreach (var memberId in members)
            {
                var role = memberId == currentProfileId ? "owner" : "member";
                await _uow.UserConversations.AddAsync(new UserConversation
                {
                    ConversationId = conversation.Id,
                    ProfileId = memberId,
                    Role = role,
                    JoinedAt = now
                });
            }

            await _uow.CompleteAsync();

            var withMembers = await _uow.Conversations.GetConversationWithMembersAsync(conversation.Id)
                             ?? conversation;

            return await MapConversationSummaryAsync(withMembers, currentProfileId);
        }

        public async Task<IEnumerable<ConversationSummaryDto>> GetConversationsAsync(
            Guid currentProfileId, int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            var conversations = await _uow.Conversations.GetConversationsForUserAsync(currentProfileId, skip, pageSize);

            var list = new List<ConversationSummaryDto>();
            foreach (var c in conversations)
            {
                list.Add(await MapConversationSummaryAsync(c, currentProfileId));
            }

            return list;
        }

        public async Task<(IEnumerable<MessageDto> Items, int Total)> GetMessagesAsync(
            Guid currentProfileId, Guid conversationId, int page, int pageSize)
        {
            // TODO: kiểm tra currentProfileId có trong conversation không
            var member = await _uow.UserConversations.GetAsync(conversationId, currentProfileId);
            if (member is null)
                throw new UnauthorizedAccessException("You are not a member of this conversation.");

            var skip = (page - 1) * pageSize;
            var (items, total) = await _uow.Messages.GetMessagesForConversationAsync(conversationId, skip, pageSize);

            var dtos = items.Select(MapMessageToDto).ToList();
            return (dtos, total);
        }

        public async Task<MessageDto> SendMessageAsync(
            Guid currentProfileId, Guid conversationId, SendMessageRequest dto)
        {
            var member = await _uow.UserConversations.GetAsync(conversationId, currentProfileId);
            if (member is null)
                throw new UnauthorizedAccessException("You are not a member of this conversation.");

            var now = DateTime.UtcNow;

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderId = currentProfileId,
                Content = dto.Content,
                MediaUrl = dto.MediaUrl,
                CreatedAt = now,
                IsEdited = false
            };

            await _uow.Messages.AddAsync(message);

            // Update conversation
            var convo = await _uow.Conversations.GetByIdAsync(conversationId);
            if (convo is not null)
            {
                convo.LastMessageAt = now;
                _uow.Conversations.Update(convo);
            }

            // Mark sender read
            member.LastReadMessageId = message.Id;
            _uow.UserConversations.Update(member);

            await _uow.CompleteAsync();

            // load sender info
            var loaded = await _uow.Messages.GetByIdAsync(message.Id);
            if (loaded is null)
                loaded = message;

            return MapMessageToDto(loaded);
        }

        public async Task MarkAsReadAsync(Guid currentProfileId, Guid conversationId, Guid lastMessageId)
        {
            var member = await _uow.UserConversations.GetAsync(conversationId, currentProfileId);
            if (member is null)
                throw new UnauthorizedAccessException("You are not a member of this conversation.");

            // đảm bảo message thuộc conversation
            var msg = await _uow.Messages.GetByIdAsync(lastMessageId);
            if (msg is null || msg.ConversationId != conversationId)
                throw new ArgumentException("Invalid message id");

            member.LastReadMessageId = lastMessageId;
            _uow.UserConversations.Update(member);
            await _uow.CompleteAsync();
        }

        // ----------------- helpers -----------------

        private async Task<ConversationSummaryDto> MapConversationSummaryAsync(
            Conversation conversation,
            Guid currentProfileId)
        {
            // Lấy last message
            var lastMessage = conversation.Messages
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault()
                ?? await _uow.Messages.GetLastMessageAsync(conversation.Id);

            var members = conversation.UserConversations;

            // Tính unread
            int unread = 0;
            var currentMember = members.FirstOrDefault(m => m.ProfileId == currentProfileId);
            if (currentMember != null)
            {
                DateTime lastReadTime = DateTime.MinValue;
                if (currentMember.LastReadMessageId.HasValue)
                {
                    var lastReadMessage = await _uow.Messages.GetByIdAsync(currentMember.LastReadMessageId.Value);
                    if (lastReadMessage != null)
                        lastReadTime = lastReadMessage.CreatedAt;
                }

                // Đếm số message mới hơn lastReadTime
                unread = await (_uow as dynamic).Messages
                    .CountAsync(conversation.Id, lastReadTime); // bạn có thể implement CountAsync riêng nếu muốn
                // hoặc đơn giản bỏ unread nếu thấy phức tạp quá
            }

            var memberDtos = members.Select(uc =>
                new ConversationMemberDto(
                    uc.ProfileId,
                    uc.Profile.Username,
                    uc.Profile.DisplayName,
                    uc.Profile.AvatarUrl
                )).ToList();

            string? preview = lastMessage?.Content ?? (lastMessage?.MediaUrl != null ? "[media]" : null);

            return new ConversationSummaryDto(
                conversation.Id,
                conversation.Type,
                conversation.LastMessageAt,
                preview,
                unread,
                memberDtos
            );
        }

        private static MessageDto MapMessageToDto(Message m)
        {
            return new MessageDto(
                m.Id,
                m.ConversationId,
                m.SenderId,
                m.Sender?.Username ?? string.Empty,
                m.Sender?.DisplayName,
                m.Sender?.AvatarUrl,
                m.Content,
                m.MediaUrl,
                m.CreatedAt,
                m.UpdatedAt,
                m.IsEdited
            );
        }
    }
}

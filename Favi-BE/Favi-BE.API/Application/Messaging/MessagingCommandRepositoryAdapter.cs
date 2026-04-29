using Favi_BE.API.Interfaces.Repositories;
using Favi_BE.API.Models.Entities;
using Favi_BE.API.Models.Entities.JoinTables;
using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Repositories;
using Favi_BE.Modules.Messaging.Application.Contracts;
using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;
using Favi_BE.Modules.Messaging.Application.Contracts.WriteModels;
using Favi_BE.Modules.Messaging.Domain;
using LegacyConversationType = Favi_BE.API.Models.Enums.ConversationType;

namespace Favi_BE.API.Application.Messaging;

internal sealed class MessagingCommandRepositoryAdapter : IMessagingCommandRepository
{
    private readonly IUnitOfWork _uow;
    private readonly IPostRepository _posts;

    public MessagingCommandRepositoryAdapter(IUnitOfWork uow, IPostRepository posts)
    {
        _uow = uow;
        _posts = posts;
    }

    public async Task<ConversationWriteData?> FindDmConversationAsync(Guid profileA, Guid profileB, CancellationToken ct = default)
    {
        var conv = await _uow.Conversations.FindDmConversationAsync(profileA, profileB);
        return conv is null ? null : MapConversation(conv);
    }

    public async Task AddConversationAsync(ConversationWriteData data, CancellationToken ct = default)
        => await _uow.Conversations.AddAsync(new Conversation
        {
            Id = data.Id,
            Type = (LegacyConversationType)(int)data.Type,
            CreatedAt = data.CreatedAt,
            LastMessageAt = data.LastMessageAt
        });

    public async Task SetConversationLastMessageAtAsync(Guid conversationId, DateTime lastMessageAt, CancellationToken ct = default)
    {
        var conv = await _uow.Conversations.GetByIdAsync(conversationId);
        if (conv is null) return;

        conv.LastMessageAt = lastMessageAt;
        _uow.Conversations.Update(conv);
    }

    public async Task AddParticipantsAsync(IReadOnlyList<ConversationParticipantData> participants, CancellationToken ct = default)
        => await _uow.UserConversations.AddRangeAsync(participants.Select(p => new UserConversation
        {
            ConversationId = p.ConversationId,
            ProfileId = p.ProfileId,
            Role = p.Role,
            JoinedAt = p.JoinedAt
        }).ToList());

    public async Task<ConversationParticipantData?> GetParticipantAsync(Guid conversationId, Guid profileId, CancellationToken ct = default)
    {
        var uc = await _uow.UserConversations.GetAsync(conversationId, profileId);
        return uc is null ? null : new ConversationParticipantData(uc.ConversationId, uc.ProfileId, uc.Role, uc.JoinedAt);
    }

    public async Task SetLastReadMessageAsync(Guid conversationId, Guid profileId, Guid lastMessageId, CancellationToken ct = default)
    {
        var uc = await _uow.UserConversations.GetAsync(conversationId, profileId);
        if (uc is null) return;

        uc.LastReadMessageId = lastMessageId;
        _uow.UserConversations.Update(uc);
    }

    public async Task AddMessageAsync(MessageWriteData data, CancellationToken ct = default)
        => await _uow.Messages.AddAsync(new Message
        {
            Id = data.Id,
            ConversationId = data.ConversationId,
            SenderId = data.SenderId,
            Content = data.Content,
            MediaUrl = data.MediaUrl,
            PostId = data.PostId,
            CreatedAt = data.CreatedAt,
            IsEdited = false
        });

    public async Task MarkMessageReadAsync(Guid messageId, Guid profileId, CancellationToken ct = default)
        => await _uow.Messages.MarkAsReadAsync(messageId, profileId);

    public async Task<ConversationSummaryReadModel?> GetConversationSummaryAsync(Guid conversationId, Guid requestingProfileId, CancellationToken ct = default)
    {
        var conv = await _uow.Conversations.GetConversationWithMembersAsync(conversationId);
        if (conv is null) return null;

        var members = conv.UserConversations?
            .Select(uc => new ConversationMemberReadModel(
                uc.ProfileId,
                uc.Profile?.Username ?? "Unknown",
                uc.Profile?.DisplayName,
                uc.Profile?.AvatarUrl,
                uc.Profile?.LastActiveAt))
            .ToList() ?? [];

        var lastMessage = await _uow.Messages.GetLastMessageAsync(conversationId);
        string? preview = null;
        if (lastMessage is not null)
        {
            if (lastMessage.PostId.HasValue) preview = "[Post]";
            else if (!string.IsNullOrWhiteSpace(lastMessage.Content))
                preview = lastMessage.Content.Length > 50 ? lastMessage.Content[..47] + "..." : lastMessage.Content;
            else preview = "[Media]";
        }

        var uc = conv.UserConversations?.FirstOrDefault(x => x.ProfileId == requestingProfileId);
        int unread = 0;
        if (uc?.LastReadMessageId is not null)
        {
            var lastRead = await _uow.Messages.GetByIdAsync(uc.LastReadMessageId.Value);
            if (lastRead is not null)
                unread = await _uow.Messages.GetUnreadCountAsync(conversationId, lastRead.CreatedAt);
        }
        else
        {
            var all = await _uow.Messages.FindAsync(m => m.ConversationId == conversationId);
            unread = all.Count();
        }

        return new ConversationSummaryReadModel(
            conv.Id,
            (ConversationType)(int)conv.Type,
            conv.LastMessageAt,
            preview,
            unread,
            members);
    }

    public async Task<MessageReadModel?> GetMessageAsync(Guid messageId, CancellationToken ct = default)
    {
        var msg = await _uow.Messages.GetByIdAsync(messageId);
        if (msg is null) return null;

        PostPreviewReadModel? postPreview = null;
        if (msg.PostId.HasValue)
        {
            var post = await _posts.GetByIdAsync(msg.PostId.Value);
            if (post is not null)
            {
                var thumb = post.PostMedias?.FirstOrDefault()?.ThumbnailUrl ?? post.PostMedias?.FirstOrDefault()?.Url;
                postPreview = new PostPreviewReadModel(
                    post.Id, post.ProfileId, post.Caption, thumb,
                    post.PostMedias?.Count ?? 0, post.CreatedAt);
            }
        }

        var readBy = msg.ReadBy?.Select(r => r.ProfileId).ToArray() ?? [];

        return new MessageReadModel(
            msg.Id, msg.ConversationId, msg.SenderId,
            msg.Sender?.Username ?? "Unknown",
            msg.Sender?.DisplayName,
            msg.Sender?.AvatarUrl,
            msg.Content, msg.MediaUrl,
            msg.CreatedAt, msg.UpdatedAt, msg.IsEdited,
            readBy, postPreview);
    }

    public async Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default)
        => await _uow.Profiles.GetByIdAsync(profileId) is not null;

    public async Task SaveAsync(CancellationToken ct = default)
        => await _uow.CompleteAsync();

    private static ConversationWriteData MapConversation(Models.Entities.Conversation c) =>
        new(c.Id, (ConversationType)(int)c.Type, c.CreatedAt, c.LastMessageAt);
}

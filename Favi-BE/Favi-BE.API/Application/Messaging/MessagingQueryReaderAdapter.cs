using Favi_BE.API.Interfaces.Repositories;
using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Repositories;
using Favi_BE.Modules.Messaging.Application.Contracts;
using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;
using Favi_BE.Modules.Messaging.Domain;

namespace Favi_BE.API.Application.Messaging;

internal sealed class MessagingQueryReaderAdapter : IMessagingQueryReader
{
    private readonly IUnitOfWork _uow;
    private readonly IPostRepository _posts;

    public MessagingQueryReaderAdapter(IUnitOfWork uow, IPostRepository posts)
    {
        _uow = uow;
        _posts = posts;
    }

    public async Task<IReadOnlyList<ConversationSummaryReadModel>> GetConversationsAsync(
        Guid profileId, int skip, int take, CancellationToken ct = default)
    {
        var conversations = await _uow.Conversations.GetConversationsForUserAsync(profileId, skip, take);
        var result = new List<ConversationSummaryReadModel>();

        foreach (var conv in conversations)
        {
            var full = await _uow.Conversations.GetConversationWithMembersAsync(conv.Id);
            if (full is null) continue;

            var members = full.UserConversations?
                .Select(uc => new ConversationMemberReadModel(
                    uc.ProfileId,
                    uc.Profile?.Username ?? "Unknown",
                    uc.Profile?.DisplayName,
                    uc.Profile?.AvatarUrl,
                    uc.Profile?.LastActiveAt))
                .ToList() ?? [];

            var lastMessage = await _uow.Messages.GetLastMessageAsync(full.Id);
            string? preview = null;
            if (lastMessage is not null)
            {
                if (lastMessage.PostId.HasValue) preview = "[Post]";
                else if (!string.IsNullOrWhiteSpace(lastMessage.Content))
                    preview = lastMessage.Content.Length > 50 ? lastMessage.Content[..47] + "..." : lastMessage.Content;
                else preview = "[Media]";
            }

            var uc = full.UserConversations?.FirstOrDefault(x => x.ProfileId == profileId);
            int unread = 0;
            if (uc?.LastReadMessageId is not null)
            {
                var lastRead = await _uow.Messages.GetByIdAsync(uc.LastReadMessageId.Value);
                if (lastRead is not null)
                    unread = await _uow.Messages.GetUnreadCountAsync(full.Id, lastRead.CreatedAt);
            }
            else
            {
                var all = await _uow.Messages.FindAsync(m => m.ConversationId == full.Id);
                unread = all.Count();
            }

            result.Add(new ConversationSummaryReadModel(
                full.Id,
                (ConversationType)(int)full.Type,
                full.LastMessageAt,
                preview,
                unread,
                members));
        }

        return result;
    }

    public async Task<(IReadOnlyList<MessageReadModel> Items, int Total)> GetMessagesAsync(
        Guid conversationId, Guid requestingProfileId, int skip, int take, CancellationToken ct = default)
    {
        var participant = await _uow.UserConversations.GetAsync(conversationId, requestingProfileId);
        if (participant is null)
            return ([], 0);

        var (messages, total) = await _uow.Messages.GetMessagesForConversationAsync(conversationId, skip, take);

        var result = new List<MessageReadModel>();
        foreach (var msg in messages)
            result.Add(await MapMessageAsync(msg));

        // Mark last-read as side effect for parity with legacy
        if (messages.Any())
        {
            participant.LastReadMessageId = messages.First().Id;
            _uow.UserConversations.Update(participant);
            await _uow.CompleteAsync();
        }

        return (result, total);
    }

    public async Task<int> GetUnreadMessagesCountAsync(Guid profileId, CancellationToken ct = default)
    {
        var conversations = await _uow.Conversations.GetConversationsForUserAsync(profileId, 0, int.MaxValue);
        int total = 0;

        foreach (var conv in conversations)
        {
            var uc = await _uow.UserConversations.GetAsync(conv.Id, profileId);
            if (uc?.LastReadMessageId is not null)
            {
                var lastRead = await _uow.Messages.GetByIdAsync(uc.LastReadMessageId.Value);
                if (lastRead is not null)
                    total += await _uow.Messages.GetUnreadCountAsync(conv.Id, lastRead.CreatedAt);
            }
            else
            {
                var all = await _uow.Messages.FindAsync(m => m.ConversationId == conv.Id);
                total += all.Count();
            }
        }

        return total;
    }

    private async Task<MessageReadModel> MapMessageAsync(API.Models.Entities.Message msg)
    {
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

        return new MessageReadModel(
            msg.Id, msg.ConversationId, msg.SenderId,
            msg.Sender?.Username ?? "Unknown",
            msg.Sender?.DisplayName,
            msg.Sender?.AvatarUrl,
            msg.Content, msg.MediaUrl,
            msg.CreatedAt, msg.UpdatedAt, msg.IsEdited,
            msg.ReadBy?.Select(r => r.ProfileId).ToArray() ?? [],
            postPreview);
    }
}

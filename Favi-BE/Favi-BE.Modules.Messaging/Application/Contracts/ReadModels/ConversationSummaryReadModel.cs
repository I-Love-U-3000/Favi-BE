using Favi_BE.Modules.Messaging.Domain;

namespace Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;

public sealed record ConversationSummaryReadModel(
    Guid Id,
    ConversationType Type,
    DateTime? LastMessageAt,
    string? LastMessagePreview,
    int UnreadCount,
    IReadOnlyList<ConversationMemberReadModel> Members);

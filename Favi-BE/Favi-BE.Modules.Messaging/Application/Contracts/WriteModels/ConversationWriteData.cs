using Favi_BE.Modules.Messaging.Domain;

namespace Favi_BE.Modules.Messaging.Application.Contracts.WriteModels;

public sealed record ConversationWriteData(
    Guid Id,
    ConversationType Type,
    DateTime CreatedAt,
    DateTime? LastMessageAt);

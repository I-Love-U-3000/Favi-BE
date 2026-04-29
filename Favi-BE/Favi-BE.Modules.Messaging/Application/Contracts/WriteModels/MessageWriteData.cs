namespace Favi_BE.Modules.Messaging.Application.Contracts.WriteModels;

public sealed record MessageWriteData(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string? Content,
    string? MediaUrl,
    Guid? PostId,
    DateTime CreatedAt);

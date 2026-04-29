namespace Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;

public sealed record MessageReadModel(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string Username,
    string? DisplayName,
    string? AvatarUrl,
    string? Content,
    string? MediaUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsEdited,
    Guid[] ReadBy,
    PostPreviewReadModel? PostPreview);

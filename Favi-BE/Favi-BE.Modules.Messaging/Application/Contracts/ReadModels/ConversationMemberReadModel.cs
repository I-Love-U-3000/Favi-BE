namespace Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;

public sealed record ConversationMemberReadModel(
    Guid ProfileId,
    string Username,
    string? DisplayName,
    string? AvatarUrl,
    DateTime? LastActiveAt);

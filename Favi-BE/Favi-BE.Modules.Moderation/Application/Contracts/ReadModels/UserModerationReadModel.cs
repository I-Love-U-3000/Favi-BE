using Favi_BE.Modules.Moderation.Domain;

namespace Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;

public sealed record UserModerationReadModel(
    Guid Id,
    Guid ProfileId,
    ModerationActionType ActionType,
    string Reason,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    DateTime? RevokedAt,
    bool Active,
    Guid AdminActionId,
    Guid AdminId
);

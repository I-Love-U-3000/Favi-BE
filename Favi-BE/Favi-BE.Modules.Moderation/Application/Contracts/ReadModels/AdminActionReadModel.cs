using Favi_BE.Modules.Moderation.Domain;

namespace Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;

public sealed record AdminActionReadModel(
    Guid Id,
    Guid AdminId,
    string? AdminUsername,
    string? AdminDisplayName,
    AdminActionType ActionType,
    Guid? TargetProfileId,
    string? TargetUsername,
    string? TargetDisplayName,
    Guid? TargetEntityId,
    string? TargetEntityType,
    Guid? ReportId,
    string? Notes,
    DateTime CreatedAt
);

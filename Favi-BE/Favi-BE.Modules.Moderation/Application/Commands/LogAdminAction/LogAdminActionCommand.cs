using Favi_BE.Modules.Moderation.Domain;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Commands.LogAdminAction;

public sealed record LogAdminActionCommand(
    Guid AdminId,
    AdminActionType ActionType,
    Guid? TargetProfileId,
    Guid? TargetEntityId,
    string? TargetEntityType,
    Guid? ReportId,
    string? Notes
) : IRequest<Guid>;

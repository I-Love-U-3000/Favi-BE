using Favi_BE.Modules.Moderation.Application.Contracts.ReadModels;
using Favi_BE.Modules.Moderation.Domain;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Commands.ModerateUser;

public sealed record ModerateUserCommand(
    Guid ProfileId,
    Guid AdminId,
    ModerationActionType ActionType,
    string Reason,
    int? DurationDays
) : IRequest<UserModerationReadModel?>;

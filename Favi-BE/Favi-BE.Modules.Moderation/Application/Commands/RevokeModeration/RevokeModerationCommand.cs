using Favi_BE.Modules.Moderation.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Moderation.Application.Commands.RevokeModeration;

public sealed record RevokeModerationCommand(
    Guid ProfileId,
    Guid AdminId,
    string? Reason
) : IRequest<ModerationCommandResult>;

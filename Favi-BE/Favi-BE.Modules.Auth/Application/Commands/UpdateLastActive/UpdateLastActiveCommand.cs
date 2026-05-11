using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.UpdateLastActive;

public sealed record UpdateLastActiveCommand(Guid ProfileId) : IRequest<DateTime>;

using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.DeleteProfile;

public sealed record DeleteProfileCommand(Guid ProfileId) : IRequest<bool>;

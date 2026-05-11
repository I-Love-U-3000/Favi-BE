using MediatR;

namespace Favi_BE.Modules.Auth.Application.Queries.GetProfileAvatar;

public sealed record GetProfileAvatarQuery(Guid ProfileId) : IRequest<string?>;

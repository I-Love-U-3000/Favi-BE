using MediatR;

namespace Favi_BE.Modules.Auth.Application.Queries.GetProfilePoster;

public sealed record GetProfilePosterQuery(Guid ProfileId) : IRequest<string?>;

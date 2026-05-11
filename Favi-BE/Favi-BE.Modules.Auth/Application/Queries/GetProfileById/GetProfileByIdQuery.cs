using Favi_BE.Modules.Auth.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Queries.GetProfileById;

public sealed record GetProfileByIdQuery(Guid ProfileId, Guid? ViewerId) : IRequest<ProfileReadModel?>;

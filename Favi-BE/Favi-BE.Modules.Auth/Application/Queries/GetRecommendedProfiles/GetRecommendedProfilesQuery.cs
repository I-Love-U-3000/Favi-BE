using Favi_BE.Modules.Auth.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Queries.GetRecommendedProfiles;

public sealed record GetRecommendedProfilesQuery(Guid ViewerId, int Skip, int Take)
    : IRequest<IReadOnlyList<ProfileReadModel>>;

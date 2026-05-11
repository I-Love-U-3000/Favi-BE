using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Queries.GetRecommendedProfiles;

internal sealed class GetRecommendedProfilesQueryHandler
    : IRequestHandler<GetRecommendedProfilesQuery, IReadOnlyList<ProfileReadModel>>
{
    private readonly IAuthQueryReader _reader;

    public GetRecommendedProfilesQueryHandler(IAuthQueryReader reader) => _reader = reader;

    public async Task<IReadOnlyList<ProfileReadModel>> Handle(
        GetRecommendedProfilesQuery request, CancellationToken cancellationToken)
    {
        if (!await _reader.ProfileExistsAsync(request.ViewerId, cancellationToken))
            return [];

        return await _reader.GetRecommendedProfilesAsync(request.ViewerId, request.Skip, request.Take, cancellationToken);
    }
}

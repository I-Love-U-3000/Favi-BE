using Favi_BE.Modules.Auth.Application.Contracts;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Queries.GetProfileInterests;

public sealed record GetProfileInterestsQuery(Guid ProfileId) : IRequest<ProfileInterestsData?>;

internal sealed class GetProfileInterestsQueryHandler(IProfileInterestsStore store)
    : IRequestHandler<GetProfileInterestsQuery, ProfileInterestsData?>
{
    public async Task<ProfileInterestsData?> Handle(GetProfileInterestsQuery request, CancellationToken ct)
        => !await store.ProfileExistsAsync(request.ProfileId, ct) ? null
            : await store.ReadAsync(request.ProfileId, ct) ?? new ProfileInterestsData([], 0);
}

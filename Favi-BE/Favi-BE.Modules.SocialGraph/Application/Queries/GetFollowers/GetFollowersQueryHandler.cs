using Favi_BE.Modules.SocialGraph.Application.Contracts;
using Favi_BE.Modules.SocialGraph.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.SocialGraph.Application.Queries.GetFollowers;

internal sealed class GetFollowersQueryHandler : IRequestHandler<GetFollowersQuery, IReadOnlyList<FollowQueryDto>>
{
    private readonly ISocialGraphQueryReader _reader;

    public GetFollowersQueryHandler(ISocialGraphQueryReader reader)
    {
        _reader = reader;
    }

    public async Task<IReadOnlyList<FollowQueryDto>> Handle(GetFollowersQuery request, CancellationToken cancellationToken)
        => await _reader.GetFollowersAsync(request.ProfileId, request.Skip, request.Take, cancellationToken);
}

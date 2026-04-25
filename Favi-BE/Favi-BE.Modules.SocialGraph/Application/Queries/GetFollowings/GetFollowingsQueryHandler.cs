using Favi_BE.Modules.SocialGraph.Application.Contracts;
using Favi_BE.Modules.SocialGraph.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.SocialGraph.Application.Queries.GetFollowings;

internal sealed class GetFollowingsQueryHandler : IRequestHandler<GetFollowingsQuery, IReadOnlyList<FollowQueryDto>>
{
    private readonly ISocialGraphQueryReader _reader;

    public GetFollowingsQueryHandler(ISocialGraphQueryReader reader)
    {
        _reader = reader;
    }

    public async Task<IReadOnlyList<FollowQueryDto>> Handle(GetFollowingsQuery request, CancellationToken cancellationToken)
        => await _reader.GetFollowingsAsync(request.ProfileId, request.Skip, request.Take, cancellationToken);
}

using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Queries.GetOnlineFriends;

internal sealed class GetOnlineFriendsQueryHandler
    : IRequestHandler<GetOnlineFriendsQuery, IReadOnlyList<ProfileReadModel>>
{
    private readonly IAuthQueryReader _reader;

    public GetOnlineFriendsQueryHandler(IAuthQueryReader reader) => _reader = reader;

    public async Task<IReadOnlyList<ProfileReadModel>> Handle(
        GetOnlineFriendsQuery request, CancellationToken cancellationToken)
    {
        if (!await _reader.ProfileExistsAsync(request.ProfileId, cancellationToken))
            return [];

        return await _reader.GetOnlineFriendsAsync(request.ProfileId, request.WithinLastMinutes, cancellationToken);
    }
}

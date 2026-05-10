using Favi_BE.Modules.Stories.Application.Contracts;
using Favi_BE.Modules.Stories.Application.Contracts.ReadModels;
using Favi_BE.Modules.Stories.Application.Exceptions;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Queries.GetActiveStoriesByProfile;

internal sealed class GetActiveStoriesByProfileQueryHandler
    : IRequestHandler<GetActiveStoriesByProfileQuery, IReadOnlyList<StoryReadModel>>
{
    private readonly IStoriesQueryReader _reader;

    public GetActiveStoriesByProfileQueryHandler(IStoriesQueryReader reader) => _reader = reader;

    public async Task<IReadOnlyList<StoryReadModel>> Handle(
        GetActiveStoriesByProfileQuery request, CancellationToken cancellationToken)
    {
        if (!await _reader.ProfileExistsAsync(request.ProfileId, cancellationToken))
            throw new ProfileNotFoundException(request.ProfileId);

        return await _reader.GetActiveStoriesByProfileAsync(request.ProfileId, request.ViewerId, cancellationToken);
    }
}

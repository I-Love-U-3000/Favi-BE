using Favi_BE.Modules.Stories.Application.Contracts;
using Favi_BE.Modules.Stories.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Queries.GetArchivedStories;

internal sealed class GetArchivedStoriesQueryHandler : IRequestHandler<GetArchivedStoriesQuery, IReadOnlyList<StoryReadModel>>
{
    private readonly IStoriesQueryReader _reader;

    public GetArchivedStoriesQueryHandler(IStoriesQueryReader reader) => _reader = reader;

    public Task<IReadOnlyList<StoryReadModel>> Handle(GetArchivedStoriesQuery request, CancellationToken cancellationToken)
        => _reader.GetArchivedStoriesAsync(request.ProfileId, cancellationToken);
}

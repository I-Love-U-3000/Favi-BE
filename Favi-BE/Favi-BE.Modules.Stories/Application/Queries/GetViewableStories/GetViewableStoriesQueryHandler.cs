using Favi_BE.Modules.Stories.Application.Contracts;
using Favi_BE.Modules.Stories.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Queries.GetViewableStories;

internal sealed class GetViewableStoriesQueryHandler : IRequestHandler<GetViewableStoriesQuery, IReadOnlyList<StoryReadModel>>
{
    private readonly IStoriesQueryReader _reader;

    public GetViewableStoriesQueryHandler(IStoriesQueryReader reader) => _reader = reader;

    public Task<IReadOnlyList<StoryReadModel>> Handle(GetViewableStoriesQuery request, CancellationToken cancellationToken)
        => _reader.GetViewableStoriesAsync(request.ViewerId, cancellationToken);
}

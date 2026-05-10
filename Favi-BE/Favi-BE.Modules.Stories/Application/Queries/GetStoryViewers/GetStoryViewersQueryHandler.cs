using Favi_BE.Modules.Stories.Application.Contracts;
using Favi_BE.Modules.Stories.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Queries.GetStoryViewers;

internal sealed class GetStoryViewersQueryHandler : IRequestHandler<GetStoryViewersQuery, IReadOnlyList<StoryViewerReadModel>>
{
    private readonly IStoriesQueryReader _reader;

    public GetStoryViewersQueryHandler(IStoriesQueryReader reader) => _reader = reader;

    public Task<IReadOnlyList<StoryViewerReadModel>> Handle(GetStoryViewersQuery request, CancellationToken cancellationToken)
        => _reader.GetViewersAsync(request.StoryId, request.RequesterId, cancellationToken);
}

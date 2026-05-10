using Favi_BE.Modules.Stories.Application.Contracts;
using Favi_BE.Modules.Stories.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Queries.GetStoryById;

internal sealed class GetStoryByIdQueryHandler : IRequestHandler<GetStoryByIdQuery, StoryReadModel?>
{
    private readonly IStoriesQueryReader _reader;

    public GetStoryByIdQueryHandler(IStoriesQueryReader reader) => _reader = reader;

    public Task<StoryReadModel?> Handle(GetStoryByIdQuery request, CancellationToken cancellationToken)
        => _reader.GetStoryByIdAsync(request.StoryId, request.ViewerId, cancellationToken);
}

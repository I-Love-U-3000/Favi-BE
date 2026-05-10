using Favi_BE.Modules.Stories.Application.Contracts;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Queries.GetActiveStoryCount;

internal sealed class GetActiveStoryCountQueryHandler : IRequestHandler<GetActiveStoryCountQuery, int>
{
    private readonly IStoriesQueryReader _reader;

    public GetActiveStoryCountQueryHandler(IStoriesQueryReader reader) => _reader = reader;

    public Task<int> Handle(GetActiveStoryCountQuery request, CancellationToken cancellationToken)
        => _reader.GetActiveStoryCountAsync(request.ProfileId, cancellationToken);
}

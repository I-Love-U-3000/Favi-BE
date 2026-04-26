using Favi_BE.Modules.Stories.Application.Contracts;
using Favi_BE.Modules.Stories.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Commands.ArchiveStory;

internal sealed class ArchiveStoryCommandHandler : IRequestHandler<ArchiveStoryCommand, StoryCommandResult>
{
    private readonly IStoriesCommandRepository _repo;

    public ArchiveStoryCommandHandler(IStoriesCommandRepository repo) => _repo = repo;

    public async Task<StoryCommandResult> Handle(ArchiveStoryCommand request, CancellationToken cancellationToken)
    {
        var story = await _repo.GetStoryForWriteAsync(request.StoryId, cancellationToken);

        if (story is null || story.ProfileId != request.RequesterId)
            return StoryCommandResult.Fail("STORY_NOT_FOUND");

        if (story.IsArchived)
            return StoryCommandResult.Ok(request.StoryId);

        await _repo.SetArchivedAsync(request.StoryId, true, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return StoryCommandResult.Ok(request.StoryId);
    }
}

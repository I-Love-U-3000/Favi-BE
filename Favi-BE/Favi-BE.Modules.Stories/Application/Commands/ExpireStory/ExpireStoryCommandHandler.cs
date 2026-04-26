using Favi_BE.Modules.Stories.Application.Contracts;
using Favi_BE.Modules.Stories.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Commands.ExpireStory;

internal sealed class ExpireStoryCommandHandler : IRequestHandler<ExpireStoryCommand, StoryCommandResult>
{
    private readonly IStoriesCommandRepository _repo;

    public ExpireStoryCommandHandler(IStoriesCommandRepository repo) => _repo = repo;

    public async Task<StoryCommandResult> Handle(ExpireStoryCommand request, CancellationToken cancellationToken)
    {
        var story = await _repo.GetStoryForWriteAsync(request.StoryId, cancellationToken);

        if (story is null)
            return StoryCommandResult.Fail("STORY_NOT_FOUND");

        await _repo.RemoveStoryAsync(request.StoryId, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        // MediaPublicId is returned so the background service can delete from external storage.
        return StoryCommandResult.Ok(request.StoryId, mediaPublicId: story.MediaPublicId);
    }
}

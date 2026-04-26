using Favi_BE.Modules.Stories.Application.Contracts;
using Favi_BE.Modules.Stories.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Commands.DeleteStory;

internal sealed class DeleteStoryCommandHandler : IRequestHandler<DeleteStoryCommand, StoryCommandResult>
{
    private readonly IStoriesCommandRepository _repo;

    public DeleteStoryCommandHandler(IStoriesCommandRepository repo) => _repo = repo;

    public async Task<StoryCommandResult> Handle(DeleteStoryCommand request, CancellationToken cancellationToken)
    {
        var story = await _repo.GetStoryForWriteAsync(request.StoryId, cancellationToken);

        if (story is null || story.ProfileId != request.RequesterId)
            return StoryCommandResult.Fail("STORY_NOT_FOUND");

        await _repo.RemoveStoryAsync(request.StoryId, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        // Return MediaPublicId so the caller can clean up external media storage.
        return StoryCommandResult.Ok(request.StoryId, mediaPublicId: story.MediaPublicId);
    }
}

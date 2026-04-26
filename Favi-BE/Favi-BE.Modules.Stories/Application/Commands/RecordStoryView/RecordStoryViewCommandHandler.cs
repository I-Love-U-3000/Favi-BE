using Favi_BE.Modules.Stories.Application.Contracts;
using Favi_BE.Modules.Stories.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Commands.RecordStoryView;

internal sealed class RecordStoryViewCommandHandler : IRequestHandler<RecordStoryViewCommand, StoryCommandResult>
{
    private readonly IStoriesCommandRepository _repo;

    public RecordStoryViewCommandHandler(IStoriesCommandRepository repo) => _repo = repo;

    public async Task<StoryCommandResult> Handle(RecordStoryViewCommand request, CancellationToken cancellationToken)
    {
        var story = await _repo.GetStoryForWriteAsync(request.StoryId, cancellationToken);

        if (story is null)
            return StoryCommandResult.Fail("STORY_NOT_FOUND");

        // Own views are not recorded.
        if (story.ProfileId == request.ViewerId)
            return StoryCommandResult.Ok(request.StoryId);

        var recorded = await _repo.RecordViewAsync(request.StoryId, request.ViewerId, cancellationToken);

        if (recorded)
            await _repo.SaveAsync(cancellationToken);

        return StoryCommandResult.Ok(request.StoryId);
    }
}

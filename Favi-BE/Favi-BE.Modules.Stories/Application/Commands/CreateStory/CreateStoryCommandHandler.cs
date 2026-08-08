using Favi_BE.Modules.Stories.Application.Contracts;
using Favi_BE.Modules.Stories.Application.Contracts.WriteModels;
using Favi_BE.Modules.Stories.Application.Responses;
using Favi_BE.Modules.Stories.Domain;
using MediatR;

namespace Favi_BE.Modules.Stories.Application.Commands.CreateStory;

internal sealed class CreateStoryCommandHandler : IRequestHandler<CreateStoryCommand, StoryCommandResult>
{
    private readonly IStoriesCommandRepository _repo;

    public CreateStoryCommandHandler(IStoriesCommandRepository repo) => _repo = repo;

    public async Task<StoryCommandResult> Handle(CreateStoryCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var id = Guid.NewGuid();

        // Create the Story aggregate root to check business rules (StoryTTL24hRule)
        var story = Story.Create(
            id,
            request.AuthorId,
            request.MediaUrl,
            request.MediaPublicId,
            request.MediaWidth,
            request.MediaHeight,
            request.MediaFormat,
            request.ThumbnailUrl,
            request.Privacy,
            now,
            now.AddHours(24)
        );

        var data = new StoryWriteData(
            Id: story.Id,
            ProfileId: story.ProfileId,
            MediaUrl: story.MediaUrl,
            MediaPublicId: story.MediaPublicId,
            MediaWidth: story.MediaWidth,
            MediaHeight: story.MediaHeight,
            MediaFormat: story.MediaFormat,
            ThumbnailUrl: story.ThumbnailUrl,
            Privacy: story.Privacy,
            IsArchived: story.IsArchived,
            IsNSFW: story.IsNSFW,
            CreatedAt: story.CreatedAt,
            ExpiresAt: story.ExpiresAt
        );

        await _repo.AddStoryAsync(data, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return StoryCommandResult.Ok(id);
    }
}

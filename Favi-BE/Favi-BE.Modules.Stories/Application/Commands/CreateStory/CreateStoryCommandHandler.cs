using Favi_BE.Modules.Stories.Application.Contracts;
using Favi_BE.Modules.Stories.Application.Contracts.WriteModels;
using Favi_BE.Modules.Stories.Application.Responses;
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

        var data = new StoryWriteData(
            Id: id,
            ProfileId: request.AuthorId,
            MediaUrl: request.MediaUrl,
            MediaPublicId: request.MediaPublicId,
            MediaWidth: request.MediaWidth,
            MediaHeight: request.MediaHeight,
            MediaFormat: request.MediaFormat,
            ThumbnailUrl: request.ThumbnailUrl,
            Privacy: request.Privacy,
            IsArchived: false,
            IsNSFW: false,
            CreatedAt: now,
            ExpiresAt: now.AddHours(24)
        );

        await _repo.AddStoryAsync(data, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return StoryCommandResult.Ok(id);
    }
}

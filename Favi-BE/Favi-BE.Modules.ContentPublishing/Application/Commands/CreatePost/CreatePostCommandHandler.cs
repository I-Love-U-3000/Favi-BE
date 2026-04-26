using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.CreatePost;

internal sealed class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, PostCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public CreatePostCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<PostCommandResult> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var postId = Guid.NewGuid();

        var post = new PostWriteData(
            Id: postId,
            ProfileId: request.AuthorId,
            Caption: request.Caption?.Trim(),
            Privacy: request.Privacy,
            LocationName: request.LocationName,
            LocationFullAddress: request.LocationFullAddress,
            LocationLatitude: request.LocationLatitude,
            LocationLongitude: request.LocationLongitude,
            CreatedAt: now,
            UpdatedAt: now,
            IsArchived: false,
            DeletedDayExpiredAt: null
        );

        await _repo.AddPostAsync(post, cancellationToken);

        if (request.TagNames is { Count: > 0 })
            await _repo.AddPostTagsAsync(postId, request.TagNames, cancellationToken);

        if (request.MediaItems is { Count: > 0 })
        {
            var mediaItems = request.MediaItems
                .Select((m, i) => new PostMediaWriteData(
                    Id: Guid.NewGuid(),
                    PostId: postId,
                    Url: m.Url,
                    ThumbnailUrl: m.ThumbnailUrl,
                    PublicId: m.PublicId,
                    Width: m.Width,
                    Height: m.Height,
                    Format: m.Format,
                    Position: i
                ));

            await _repo.AddPostMediaRangeAsync(mediaItems, cancellationToken);
        }

        await _repo.SaveAsync(cancellationToken);

        return PostCommandResult.Ok(postId);
    }
}

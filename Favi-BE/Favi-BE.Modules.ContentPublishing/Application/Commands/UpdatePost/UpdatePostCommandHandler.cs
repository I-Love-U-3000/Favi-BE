using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Favi_BE.Modules.ContentPublishing.Application.Responses;
using Favi_BE.Modules.ContentPublishing.Domain;
using MediatR;

namespace Favi_BE.Modules.ContentPublishing.Application.Commands.UpdatePost;

internal sealed class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, PostCommandResult>
{
    private readonly IContentPublishingCommandRepository _repo;

    public UpdatePostCommandHandler(IContentPublishingCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<PostCommandResult> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
    {
        var postData = await _repo.GetPostForWriteAsync(request.PostId, cancellationToken);
        if (postData is null)
            return PostCommandResult.Fail("POST_NOT_FOUND", "Bài viết không tồn tại.");

        // Reconstitute the Domain Post aggregate root
        var post = new ContentPublishing.Domain.Post(
            postData.Id,
            postData.ProfileId,
            postData.Caption,
            postData.Privacy,
            postData.CreatedAt,
            postData.UpdatedAt,
            postData.LocationName,
            postData.LocationFullAddress,
            postData.LocationLatitude,
            postData.LocationLongitude,
            postData.DeletedDayExpiredAt,
            postData.IsArchived,
            isNSFW: false
        );

        // Execute domain aggregate modification and rule validation
        post.Update(
            request.RequesterId,
            request.Caption is not null ? request.Caption.Trim() : post.Caption,
            request.Privacy ?? post.Privacy
        );

        var updated = postData with
        {
            Caption = post.Caption,
            Privacy = post.Privacy,
            UpdatedAt = post.UpdatedAt
        };

        await _repo.UpdatePostAsync(updated, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return PostCommandResult.Ok(request.PostId);
    }
}

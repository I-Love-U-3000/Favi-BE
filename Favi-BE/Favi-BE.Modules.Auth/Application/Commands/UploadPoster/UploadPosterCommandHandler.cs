using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.Auth.Application.Commands.UploadPoster;

internal sealed class UploadPosterCommandHandler : IRequestHandler<UploadPosterCommand, SavedImageResult>
{
    private readonly IAuthWriteRepository _repo;

    public UploadPosterCommandHandler(IAuthWriteRepository repo) => _repo = repo;

    public async Task<SavedImageResult> Handle(UploadPosterCommand request, CancellationToken cancellationToken)
    {
        if (!await _repo.ProfileExistsAsync(request.ProfileId, cancellationToken))
            return SavedImageResult.Fail("PROFILE_NOT_FOUND");

        var img = request.Image;
        var (mediaId, url, publicId, width, height, format, thumbnailUrl) =
            await _repo.SavePosterAsync(
                request.ProfileId,
                img.Url, img.ThumbnailUrl, img.PublicId,
                img.Width, img.Height, img.Format,
                cancellationToken);

        await _repo.SaveAsync(cancellationToken);

        return SavedImageResult.Success(mediaId, url, publicId, width, height, format, thumbnailUrl);
    }
}

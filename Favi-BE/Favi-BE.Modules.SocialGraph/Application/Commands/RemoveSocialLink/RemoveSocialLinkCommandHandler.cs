using Favi_BE.Modules.SocialGraph.Application.Contracts;
using Favi_BE.Modules.SocialGraph.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.SocialGraph.Application.Commands.RemoveSocialLink;

internal sealed class RemoveSocialLinkCommandHandler : IRequestHandler<RemoveSocialLinkCommand, SocialLinkCommandResult>
{
    private readonly ISocialGraphCommandRepository _repo;

    public RemoveSocialLinkCommandHandler(ISocialGraphCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<SocialLinkCommandResult> Handle(RemoveSocialLinkCommand request, CancellationToken cancellationToken)
    {
        var link = await _repo.GetSocialLinkByIdAsync(request.LinkId, cancellationToken);

        if (link is null || link.ProfileId != request.ProfileId)
            return SocialLinkCommandResult.Fail("SOCIAL_LINK_NOT_FOUND", "Không tìm thấy liên kết để xoá.");

        await _repo.RemoveSocialLinkAsync(request.LinkId, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return SocialLinkCommandResult.Success();
    }
}

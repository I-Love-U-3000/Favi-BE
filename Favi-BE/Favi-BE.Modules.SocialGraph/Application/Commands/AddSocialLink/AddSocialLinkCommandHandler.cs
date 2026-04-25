using Favi_BE.Modules.SocialGraph.Application.Contracts;
using Favi_BE.Modules.SocialGraph.Application.Contracts.ReadModels;
using Favi_BE.Modules.SocialGraph.Application.Contracts.WriteModels;
using Favi_BE.Modules.SocialGraph.Application.Responses;
using MediatR;

namespace Favi_BE.Modules.SocialGraph.Application.Commands.AddSocialLink;

internal sealed class AddSocialLinkCommandHandler : IRequestHandler<AddSocialLinkCommand, SocialLinkCommandResult>
{
    private readonly ISocialGraphCommandRepository _repo;

    public AddSocialLinkCommandHandler(ISocialGraphCommandRepository repo)
    {
        _repo = repo;
    }

    public async Task<SocialLinkCommandResult> Handle(AddSocialLinkCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var writeData = new SocialLinkWriteData(id, request.ProfileId, request.Kind, request.Url, now);

        await _repo.AddSocialLinkAsync(writeData, cancellationToken);
        await _repo.SaveAsync(cancellationToken);

        return SocialLinkCommandResult.Success(
            new SocialLinkQueryDto(id, request.ProfileId, request.Kind, request.Url, now));
    }
}

using Favi_BE.Modules.SocialGraph.Application.Contracts;
using Favi_BE.Modules.SocialGraph.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.SocialGraph.Application.Queries.GetSocialLinks;

internal sealed class GetSocialLinksQueryHandler : IRequestHandler<GetSocialLinksQuery, IReadOnlyList<SocialLinkQueryDto>>
{
    private readonly ISocialGraphQueryReader _reader;

    public GetSocialLinksQueryHandler(ISocialGraphQueryReader reader)
    {
        _reader = reader;
    }

    public async Task<IReadOnlyList<SocialLinkQueryDto>> Handle(GetSocialLinksQuery request, CancellationToken cancellationToken)
        => await _reader.GetSocialLinksAsync(request.ProfileId, cancellationToken);
}

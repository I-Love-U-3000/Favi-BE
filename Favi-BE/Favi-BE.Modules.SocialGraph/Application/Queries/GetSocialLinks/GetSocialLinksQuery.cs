using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.SocialGraph.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.SocialGraph.Application.Queries.GetSocialLinks;

public sealed record GetSocialLinksQuery(Guid ProfileId) : IQuery<IReadOnlyList<SocialLinkQueryDto>>;

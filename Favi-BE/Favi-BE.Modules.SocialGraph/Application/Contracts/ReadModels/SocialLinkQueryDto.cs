using Favi_BE.Modules.SocialGraph.Domain;

namespace Favi_BE.Modules.SocialGraph.Application.Contracts.ReadModels;

public sealed record SocialLinkQueryDto(
    Guid Id,
    Guid ProfileId,
    SocialKind Kind,
    string Url,
    DateTime CreatedAt);

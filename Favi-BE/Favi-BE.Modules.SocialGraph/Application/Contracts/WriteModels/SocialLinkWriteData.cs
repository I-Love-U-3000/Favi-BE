using Favi_BE.Modules.SocialGraph.Domain;

namespace Favi_BE.Modules.SocialGraph.Application.Contracts.WriteModels;

public sealed record SocialLinkWriteData(
    Guid Id,
    Guid ProfileId,
    SocialKind Kind,
    string Url,
    DateTime CreatedAt);

using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.SocialGraph.Application.Responses;
using Favi_BE.Modules.SocialGraph.Domain;

namespace Favi_BE.Modules.SocialGraph.Application.Commands.AddSocialLink;

public sealed record AddSocialLinkCommand(
    Guid ProfileId,
    SocialKind Kind,
    string Url) : ICommand<SocialLinkCommandResult>;

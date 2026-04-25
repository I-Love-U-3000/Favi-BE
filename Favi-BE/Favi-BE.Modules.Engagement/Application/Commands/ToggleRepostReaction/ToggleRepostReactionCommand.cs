using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Engagement.Application.Responses;
using Favi_BE.Modules.Engagement.Domain;

namespace Favi_BE.Modules.Engagement.Application.Commands.ToggleRepostReaction;

public sealed record ToggleRepostReactionCommand(
    Guid RepostId,
    Guid ActorId,
    ReactionType Type) : ICommand<ReactionCommandResult>;

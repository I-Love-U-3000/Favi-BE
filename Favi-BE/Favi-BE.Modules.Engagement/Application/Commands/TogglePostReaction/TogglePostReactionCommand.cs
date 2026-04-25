using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Engagement.Application.Responses;
using Favi_BE.Modules.Engagement.Domain;

namespace Favi_BE.Modules.Engagement.Application.Commands.TogglePostReaction;

public sealed record TogglePostReactionCommand(
    Guid PostId,
    Guid ActorId,
    ReactionType Type) : ICommand<ReactionCommandResult>;

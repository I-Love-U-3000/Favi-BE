using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Engagement.Application.Responses;
using Favi_BE.Modules.Engagement.Domain;

namespace Favi_BE.Modules.Engagement.Application.Commands.ToggleCollectionReaction;

public sealed record ToggleCollectionReactionCommand(
    Guid CollectionId,
    Guid ActorId,
    ReactionType Type) : ICommand<ReactionCommandResult>;

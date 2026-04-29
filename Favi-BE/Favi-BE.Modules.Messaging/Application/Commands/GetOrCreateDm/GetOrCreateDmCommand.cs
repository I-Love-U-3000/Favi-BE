using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Messaging.Application.Commands.GetOrCreateDm;

public sealed record GetOrCreateDmCommand(
    Guid CurrentProfileId,
    Guid OtherProfileId) : ICommand<ConversationSummaryReadModel?>;

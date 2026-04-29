using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Messaging.Application.Commands.CreateGroupConversation;

public sealed record CreateGroupConversationCommand(
    Guid CreatorProfileId,
    IReadOnlyList<Guid> MemberIds) : ICommand<ConversationSummaryReadModel?>;

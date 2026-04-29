using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Messaging.Application.Commands.SendMessage;

public sealed record SendMessageCommand(
    Guid SenderProfileId,
    Guid ConversationId,
    string? Content,
    string? MediaUrl,
    Guid? PostId) : ICommand<MessageReadModel?>;

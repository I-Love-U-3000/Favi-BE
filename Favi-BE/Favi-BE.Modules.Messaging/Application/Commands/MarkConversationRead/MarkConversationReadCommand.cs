using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Messaging.Application.Responses;

namespace Favi_BE.Modules.Messaging.Application.Commands.MarkConversationRead;

public sealed record MarkConversationReadCommand(
    Guid ProfileId,
    Guid ConversationId,
    Guid LastMessageId) : ICommand<MessagingCommandResult>;

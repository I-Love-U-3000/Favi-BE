namespace Favi_BE.Modules.Messaging.Application.Contracts.WriteModels;

public sealed record ConversationParticipantData(
    Guid ConversationId,
    Guid ProfileId,
    string Role,
    DateTime JoinedAt);

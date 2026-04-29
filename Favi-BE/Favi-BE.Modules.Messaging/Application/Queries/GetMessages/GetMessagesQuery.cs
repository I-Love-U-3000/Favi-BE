using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Messaging.Application.Queries.GetMessages;

public sealed record GetMessagesQuery(
    Guid ConversationId,
    Guid RequestingProfileId,
    int Page,
    int PageSize) : IQuery<(IReadOnlyList<MessageReadModel> Items, int Total)>;

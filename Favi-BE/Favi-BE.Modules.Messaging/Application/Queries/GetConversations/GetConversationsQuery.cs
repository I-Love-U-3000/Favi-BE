using Favi_BE.BuildingBlocks.Application.Messaging;
using Favi_BE.Modules.Messaging.Application.Contracts.ReadModels;

namespace Favi_BE.Modules.Messaging.Application.Queries.GetConversations;

public sealed record GetConversationsQuery(
    Guid ProfileId,
    int Page,
    int PageSize) : IQuery<IReadOnlyList<ConversationSummaryReadModel>>;

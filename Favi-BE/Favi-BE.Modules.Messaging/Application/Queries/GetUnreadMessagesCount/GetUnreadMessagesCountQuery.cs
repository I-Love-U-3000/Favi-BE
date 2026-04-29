using Favi_BE.BuildingBlocks.Application.Messaging;

namespace Favi_BE.Modules.Messaging.Application.Queries.GetUnreadMessagesCount;

public sealed record GetUnreadMessagesCountQuery(Guid ProfileId) : IQuery<int>;

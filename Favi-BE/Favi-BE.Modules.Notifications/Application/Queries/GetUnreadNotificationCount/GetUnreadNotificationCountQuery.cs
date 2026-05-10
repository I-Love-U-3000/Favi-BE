using MediatR;

namespace Favi_BE.Modules.Notifications.Application.Queries.GetUnreadNotificationCount;

public sealed record GetUnreadNotificationCountQuery(Guid RecipientId) : IRequest<int>;

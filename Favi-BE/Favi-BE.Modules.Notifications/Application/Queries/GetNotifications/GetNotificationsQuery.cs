using Favi_BE.Modules.Notifications.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Notifications.Application.Queries.GetNotifications;

public sealed record GetNotificationsQuery(Guid RecipientId, int Page, int PageSize)
    : IRequest<(IReadOnlyList<NotificationReadModel> Items, int TotalCount)>;

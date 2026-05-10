using Favi_BE.Modules.Notifications.Application.Contracts;
using Favi_BE.Modules.Notifications.Application.Contracts.ReadModels;
using MediatR;

namespace Favi_BE.Modules.Notifications.Application.Queries.GetNotifications;

internal sealed class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, (IReadOnlyList<NotificationReadModel> Items, int TotalCount)>
{
    private readonly INotificationQueryReader _reader;

    public GetNotificationsQueryHandler(INotificationQueryReader reader) => _reader = reader;

    public Task<(IReadOnlyList<NotificationReadModel> Items, int TotalCount)> Handle(
        GetNotificationsQuery request, CancellationToken cancellationToken)
        => _reader.GetNotificationsAsync(request.RecipientId, request.Page, request.PageSize, cancellationToken);
}

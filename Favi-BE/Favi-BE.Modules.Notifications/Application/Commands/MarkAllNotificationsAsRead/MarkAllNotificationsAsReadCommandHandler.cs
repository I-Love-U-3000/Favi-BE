using Favi_BE.Modules.Notifications.Application.Contracts;
using MediatR;

namespace Favi_BE.Modules.Notifications.Application.Commands.MarkAllNotificationsAsRead;

internal sealed class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand>
{
    private readonly INotificationCommandRepository _repository;
    private readonly INotificationRealtimeGateway _gateway;

    public MarkAllNotificationsAsReadCommandHandler(
        INotificationCommandRepository repository,
        INotificationRealtimeGateway gateway)
    {
        _repository = repository;
        _gateway = gateway;
    }

    public async Task Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        await _repository.MarkAllAsReadAsync(request.RecipientId, cancellationToken);
        await _gateway.SendUnreadCountAsync(request.RecipientId, 0, cancellationToken);
    }
}

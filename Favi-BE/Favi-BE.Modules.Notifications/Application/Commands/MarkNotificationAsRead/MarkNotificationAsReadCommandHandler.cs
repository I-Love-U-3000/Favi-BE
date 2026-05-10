using Favi_BE.Modules.Notifications.Application.Contracts;
using MediatR;

namespace Favi_BE.Modules.Notifications.Application.Commands.MarkNotificationAsRead;

internal sealed class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, bool>
{
    private readonly INotificationCommandRepository _repository;

    public MarkNotificationAsReadCommandHandler(INotificationCommandRepository repository)
        => _repository = repository;

    public Task<bool> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
        => _repository.MarkAsReadAsync(request.NotificationId, request.RecipientId, cancellationToken);
}

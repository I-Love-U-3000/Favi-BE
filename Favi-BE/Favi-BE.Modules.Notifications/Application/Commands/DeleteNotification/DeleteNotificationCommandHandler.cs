using Favi_BE.Modules.Notifications.Application.Contracts;
using MediatR;

namespace Favi_BE.Modules.Notifications.Application.Commands.DeleteNotification;

internal sealed class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, bool>
{
    private readonly INotificationCommandRepository _repository;

    public DeleteNotificationCommandHandler(INotificationCommandRepository repository)
        => _repository = repository;

    public Task<bool> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
        => _repository.DeleteAsync(request.NotificationId, request.RecipientId, cancellationToken);
}

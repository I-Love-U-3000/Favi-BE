using Favi_BE.Modules.Notifications.Application.Contracts;
using MediatR;

namespace Favi_BE.Modules.Notifications.Application.Commands.DeleteAllReadNotifications;

internal sealed class DeleteAllReadNotificationsCommandHandler : IRequestHandler<DeleteAllReadNotificationsCommand, int>
{
    private readonly INotificationCommandRepository _repository;

    public DeleteAllReadNotificationsCommandHandler(INotificationCommandRepository repository)
        => _repository = repository;

    public Task<int> Handle(DeleteAllReadNotificationsCommand request, CancellationToken cancellationToken)
        => _repository.DeleteAllReadAsync(request.RecipientId, cancellationToken);
}

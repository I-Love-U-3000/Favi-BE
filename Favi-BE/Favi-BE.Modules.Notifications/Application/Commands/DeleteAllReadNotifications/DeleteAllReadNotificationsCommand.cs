using MediatR;

namespace Favi_BE.Modules.Notifications.Application.Commands.DeleteAllReadNotifications;

public sealed record DeleteAllReadNotificationsCommand(Guid RecipientId) : IRequest<int>;

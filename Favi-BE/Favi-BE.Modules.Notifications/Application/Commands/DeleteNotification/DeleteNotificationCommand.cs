using MediatR;

namespace Favi_BE.Modules.Notifications.Application.Commands.DeleteNotification;

public sealed record DeleteNotificationCommand(Guid NotificationId, Guid RecipientId) : IRequest<bool>;

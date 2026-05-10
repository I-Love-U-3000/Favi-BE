using MediatR;

namespace Favi_BE.Modules.Notifications.Application.Commands.MarkNotificationAsRead;

public sealed record MarkNotificationAsReadCommand(Guid NotificationId, Guid RecipientId) : IRequest<bool>;

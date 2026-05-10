using MediatR;

namespace Favi_BE.Modules.Notifications.Application.Commands.MarkAllNotificationsAsRead;

public sealed record MarkAllNotificationsAsReadCommand(Guid RecipientId) : IRequest;

using Favi_BE.API.Modules.Notifications;
using Favi_BE.BuildingBlocks.Application.Inbox;
using Favi_BE.Modules.Notifications.Application.Consumers;
using Favi_BE.Modules.Notifications.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.API.DependencyInjection;

public static class NotificationsModuleDiExtensions
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddScoped<INotificationWriteRepository, NotificationWriteRepositoryAdapter>();
        services.AddScoped<INotificationRealtimeGateway, NotificationRealtimeGatewayAdapter>();
        services.AddScoped<INotificationQueryReader, NotificationQueryReaderAdapter>();
        services.AddScoped<INotificationCommandRepository, NotificationCommandRepositoryAdapter>();

        // Inbox consumers for OutboxProcessor / InboxProcessor
        services.AddScoped<IInboxConsumer, CommentCreatedNotificationConsumer>();
        services.AddScoped<IInboxConsumer, PostReactionToggledNotificationConsumer>();
        services.AddScoped<IInboxConsumer, CommentReactionToggledNotificationConsumer>();
        services.AddScoped<IInboxConsumer, UserFollowedNotificationConsumer>();

        return services;
    }
}

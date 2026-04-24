using Favi_BE.BuildingBlocks.Application.Inbox;
using Favi_BE.Modules.Notifications.Application.Consumers;
using Favi_BE.Modules.Notifications.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.Modules.Notifications;

public static class NotificationsModuleExtensions
{
    /// <summary>
    /// Registers all Notifications module services.
    /// Port adapters (INotificationWriteRepository, INotificationRealtimeGateway)
    /// must be registered in the host project (Favi-BE.API) before calling this.
    /// </summary>
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        // Inbox consumers — each handles one integration event type from the Outbox
        services.AddScoped<IInboxConsumer, CommentCreatedNotificationConsumer>();
        services.AddScoped<IInboxConsumer, UserFollowedNotificationConsumer>();
        services.AddScoped<IInboxConsumer, PostReactionToggledNotificationConsumer>();
        services.AddScoped<IInboxConsumer, CommentReactionToggledNotificationConsumer>();

        return services;
    }
}

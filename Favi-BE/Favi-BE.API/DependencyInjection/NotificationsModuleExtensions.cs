using Favi_BE.API.Modules.Notifications;
using Favi_BE.Modules.Notifications.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.API.DependencyInjection;

public static class NotificationsModuleDiExtensions
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddScoped<INotificationWriteRepository, NotificationWriteRepositoryAdapter>();
        services.AddScoped<INotificationRealtimeGateway, NotificationRealtimeGatewayAdapter>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Favi_BE.Modules.Notifications.AssemblyReference.Assembly);
        });

        return services;
    }
}

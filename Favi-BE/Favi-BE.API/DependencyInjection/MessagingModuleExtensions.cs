using Favi_BE.API.Application.Messaging;
using Favi_BE.Modules.Messaging;
using Favi_BE.Modules.Messaging.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.API.DependencyInjection;

public static class MessagingModuleDiExtensions
{
    public static IServiceCollection AddMessagingModule(this IServiceCollection services)
    {
        services.AddScoped<IMessagingCommandRepository, MessagingCommandRepositoryAdapter>();
        services.AddScoped<IMessagingQueryReader, MessagingQueryReaderAdapter>();
        services.AddScoped<IChatRealtimeGateway, ChatRealtimeGatewayAdapter>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Favi_BE.Modules.Messaging.AssemblyReference.Assembly);
        });

        return services;
    }
}

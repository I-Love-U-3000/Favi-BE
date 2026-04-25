using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.Modules.SocialGraph;

public static class SocialGraphModuleExtensions
{
    /// <summary>
    /// Registers the Social Graph module MediatR handlers.
    /// Port adapters (ISocialGraphCommandRepository, ISocialGraphQueryReader, ISocialGraphNotificationService)
    /// must be registered in the host project (Favi-BE.API) before calling this.
    /// </summary>
    public static IServiceCollection AddSocialGraphModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
        });

        return services;
    }
}

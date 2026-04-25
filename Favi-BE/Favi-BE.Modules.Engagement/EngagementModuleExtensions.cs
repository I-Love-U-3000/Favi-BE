using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.Modules.Engagement;

public static class EngagementModuleExtensions
{
    /// <summary>
    /// Registers the Engagement module MediatR handlers.
    /// Port adapters (IEngagementCommandRepository, IEngagementQueryReader, IEngagementNotificationService)
    /// must be registered in the host project (Favi-BE.API) before calling this.
    /// </summary>
    public static IServiceCollection AddEngagementModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
        });

        return services;
    }
}

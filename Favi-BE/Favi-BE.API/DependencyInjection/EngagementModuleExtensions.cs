using Favi_BE.API.Application.Engagement;
using Favi_BE.Modules.Engagement;
using Favi_BE.Modules.Engagement.Application.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.API.DependencyInjection;

public static class EngagementModuleDiExtensions
{
    /// <summary>
    /// Registers the Engagement module: port adapters + MediatR handler scan.
    /// Called from AddApplicationExtensions to keep Program.cs clean.
    /// </summary>
    public static IServiceCollection AddEngagementModule(this IServiceCollection services)
    {
        // Port adapters (module ports → existing infrastructure)
        services.AddScoped<IEngagementCommandRepository, EngagementCommandRepositoryAdapter>();
        services.AddScoped<IEngagementQueryReader, EngagementQueryReaderAdapter>();
        services.AddScoped<IEngagementNotificationService, EngagementNotificationServiceAdapter>();

        // Scan the Engagement module assembly for MediatR handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Favi_BE.Modules.Engagement.AssemblyReference.Assembly);
        });

        return services;
    }
}

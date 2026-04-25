using Favi_BE.API.Application.SocialGraph;
using Favi_BE.Modules.SocialGraph;
using Favi_BE.Modules.SocialGraph.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.API.DependencyInjection;

public static class SocialGraphModuleDiExtensions
{
    /// <summary>
    /// Registers the Social Graph module: port adapters + MediatR handler scan.
    /// Called from AddApplicationExtensions to keep Program.cs clean.
    /// </summary>
    public static IServiceCollection AddSocialGraphModule(this IServiceCollection services)
    {
        services.AddScoped<ISocialGraphCommandRepository, SocialGraphCommandRepositoryAdapter>();
        services.AddScoped<ISocialGraphQueryReader, SocialGraphQueryReaderAdapter>();
        services.AddScoped<ISocialGraphNotificationService, SocialGraphNotificationServiceAdapter>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Favi_BE.Modules.SocialGraph.AssemblyReference.Assembly);
        });

        return services;
    }
}

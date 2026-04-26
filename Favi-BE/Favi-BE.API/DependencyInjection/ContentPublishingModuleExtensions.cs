using Favi_BE.API.Application.ContentPublishing;
using Favi_BE.Modules.ContentPublishing;
using Favi_BE.Modules.ContentPublishing.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.API.DependencyInjection;

public static class ContentPublishingModuleDiExtensions
{
    /// <summary>
    /// Registers the Content Publishing module: port adapters + MediatR handler scan.
    /// Called from AddApplicationExtensions to keep Program.cs clean.
    /// </summary>
    public static IServiceCollection AddContentPublishingModule(this IServiceCollection services)
    {
        services.AddScoped<IContentPublishingCommandRepository, ContentPublishingCommandRepositoryAdapter>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
        });

        return services;
    }
}

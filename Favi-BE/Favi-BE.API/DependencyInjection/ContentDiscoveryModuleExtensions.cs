using Favi_BE.API.Application.ContentDiscovery;
using Favi_BE.Modules.ContentDiscovery;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.API.DependencyInjection;

public static class ContentDiscoveryModuleDiExtensions
{
    public static IServiceCollection AddContentDiscoveryModule(this IServiceCollection services)
    {
        services.AddScoped<IContentDiscoveryQueryReader, ContentDiscoveryQueryReaderAdapter>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
        });

        return services;
    }
}

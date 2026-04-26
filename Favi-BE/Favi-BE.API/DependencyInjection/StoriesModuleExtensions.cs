using Favi_BE.API.Application.Stories;
using Favi_BE.Modules.Stories;
using Favi_BE.Modules.Stories.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.API.DependencyInjection;

public static class StoriesModuleDiExtensions
{
    public static IServiceCollection AddStoriesModule(this IServiceCollection services)
    {
        services.AddScoped<IStoriesCommandRepository, StoriesCommandRepositoryAdapter>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
        });

        return services;
    }
}

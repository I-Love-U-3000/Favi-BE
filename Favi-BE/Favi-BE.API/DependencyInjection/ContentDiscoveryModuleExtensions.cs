using Favi_BE.API.Application.ContentDiscovery;
using Favi_BE.Modules.ContentDiscovery;
using Favi_BE.Modules.ContentDiscovery.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Favi_BE.API.DependencyInjection;

public static class ContentDiscoveryModuleDiExtensions
{
    public static IServiceCollection AddContentDiscoveryModule(this IServiceCollection services)
    {
        services.AddScoped<IContentDiscoveryQueryReader, ContentDiscoveryQueryReaderAdapter>();
        services.AddScoped<IColdStartQueryReader, ColdStartQueryReader>();
        services.AddScoped<IGraphCandidateSource, NoGraphCandidateSource>();
        services.AddOptions<ExperimentalFeedOptions>().BindConfiguration("ExperimentalFeed");
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<ExperimentalFeedSnapshot>();
        services.AddHostedService<ExperimentalFeedStartupValidator>();
        services.AddScoped<IExperimentalFeedQueryReader, ExperimentalFeedQueryReader>();



        return services;
    }
}

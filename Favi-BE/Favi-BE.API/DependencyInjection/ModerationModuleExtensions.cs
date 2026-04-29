using Favi_BE.API.Application.Moderation;
using Favi_BE.Modules.Moderation.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.API.DependencyInjection;

public static class ModerationModuleDiExtensions
{
    public static IServiceCollection AddModerationModule(this IServiceCollection services)
    {
        services.AddScoped<IModerationCommandRepository, ModerationCommandRepositoryAdapter>();
        services.AddScoped<IModerationQueryReader, ModerationQueryReaderAdapter>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Favi_BE.Modules.Moderation.AssemblyReference.Assembly);
        });

        return services;
    }
}

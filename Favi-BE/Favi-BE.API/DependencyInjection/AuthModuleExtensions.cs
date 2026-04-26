using Favi_BE.API.Application.Auth;
using Favi_BE.Modules.Auth.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.API.DependencyInjection;

public static class AuthModuleDiExtensions
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        services.AddScoped<IAuthWriteRepository, AuthWriteRepositoryAdapter>();
        services.AddScoped<IJwtTokenService, JwtTokenServiceAdapter>();
        services.AddScoped<IAuthQueryReader, AuthQueryReaderAdapter>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Favi_BE.Modules.Auth.AssemblyReference.Assembly);
        });

        return services;
    }
}

using Favi_BE.API.Application.Auth;
using Favi_BE.Modules.Auth;
using Favi_BE.Modules.Auth.Application.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.API.DependencyInjection;

public static class AuthModuleExtensions
{
    /// <summary>
    /// Registers the Auth module: port adapters + MediatR handler scan.
    /// Called from AddApplicationExtensions to keep Program.cs clean.
    /// </summary>
    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        // Register port adapters (module ports → existing infrastructure)
        services.AddScoped<IAuthWriteRepository, AuthWriteRepositoryAdapter>();
        services.AddScoped<IJwtTokenService, JwtTokenServiceAdapter>();
        services.AddScoped<IAuthQueryReader, AuthQueryReaderAdapter>();

        // Scan the Auth module assembly for MediatR handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Favi_BE.Modules.Auth.AssemblyReference.Assembly);
        });

        return services;
    }
}

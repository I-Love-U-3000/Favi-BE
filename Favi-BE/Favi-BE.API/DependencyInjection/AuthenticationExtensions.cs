using System.Security.Claims;
using System.Text;
using Favi_BE.Authorization;
using Favi_BE.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Favi_BE.API.DependencyInjection;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAuthenticationExtensions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtOpt = configuration.GetSection("Jwt").Get<JwtOptions>()!;
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpt.Key));

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOpt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOpt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("JWT auth failed: " + context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var claims = context.Principal?.Claims.Select(c => $"{c.Type}:{c.Value}");
                        Console.WriteLine("Token validated: " + string.Join(", ", claims ?? []));
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddSingleton<IAuthorizationHandler, RequireAdminHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireUser", policy => policy.RequireClaim(ClaimTypes.Role, new[] { "user", "moderator", "admin" }));
            options.AddPolicy(AdminPolicies.RequireAdmin, policy =>
                policy.Requirements.Add(new RequireAdminRequirement()));
        });

        return services;
    }
}

using System;
using Favi_BE.API.HealthChecks;
using Favi_BE.API.Services;
using Favi_BE.BuildingBlocks.Infrastructure.Inbox;
using Favi_BE.BuildingBlocks.Infrastructure.Outbox;
using Favi_BE.Data;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Favi_BE.API.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructureExtensions(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:3000",
                        "http://127.0.0.1:3000",
                        "https://localhost:3000",
                        "https://127.0.0.1:3000",
                        "http://localhost:5000",
                        "http://127.0.0.1:5000"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.Configure<MemoryHealthCheckOptions>(options =>
        {
            options.ThresholdMB = 1024;
            options.DegradedThresholdMB = 512;
        });

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(
                "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "postgresql", "ready"])
            .AddCheck<MemoryHealthCheck>(
                "memory",
                failureStatus: HealthStatus.Degraded,
                tags: ["system", "memory"])
            .AddCheck<VectorIndexHealthCheck>(
                "vector-index-api",
                failureStatus: HealthStatus.Degraded,
                tags: ["external", "vector", "ai"])
            .AddCheck<QdrantHealthCheck>(
                "qdrant",
                failureStatus: HealthStatus.Degraded,
                tags: ["external", "vector", "database"])
            .AddCheck<RedisHealthCheck>(
                "redis",
                failureStatus: HealthStatus.Degraded,
                tags: ["external", "cache"]);

        services.AddHostedService<PostCleanupService>();
        services.AddHostedService<StoryExpirationService>();
        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<InboxProcessor>();

        services.AddSignalR();

        services.Configure<VectorIndexOptions>(configuration.GetSection("VectorIndex"));
        services.AddHttpClient<IVectorIndexService, VectorIndexService>(client =>
        {
            var vectorConfig = configuration.GetSection("VectorIndex");
            var baseUrl = vectorConfig["BaseUrl"] ?? "http://vector-index-api:8080";
            var timeoutSeconds = int.Parse(vectorConfig["TimeoutSeconds"] ?? "60");
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.Configure<NSFWOptions>(configuration.GetSection("NSFW"));
        services.AddHttpClient<INSFWService, NSFWService>(client =>
        {
            var nsfwConfig = configuration.GetSection("NSFW");
            var baseUrl = nsfwConfig["BaseUrl"] ?? "http://vector-index-api:8080";
            var timeoutSeconds = int.Parse(nsfwConfig["TimeoutSeconds"] ?? "30");
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"), npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null
                );
            });

            options.EnableSensitiveDataLogging(environment.IsDevelopment());
            options.EnableDetailedErrors(environment.IsDevelopment());
        });

        return services;
    }
}

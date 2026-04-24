using Favi_BE.API.DependencyInjection;
using Favi_BE.API.Hubs;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthenticationExtensions(builder.Configuration)
    .AddInfrastructureExtensions(builder.Configuration, builder.Environment)
    .AddApplicationExtensions(builder.Configuration);

var app = builder.Build();

await app.ApplyDatabaseMigrationsAndSeedAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseMiddleware<Favi_BE.API.Middleware.ExceptionHandlingMiddleware>();

app.UseCors("Frontend");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ============================================
// HEALTH CHECK ENDPOINTS
// ============================================

// Basic health check - returns simple status (for load balancers, container orchestration)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false, // Don't run any checks, just return healthy if app is running
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = "ok",
            timestamp = DateTime.UtcNow
        });
    }
}).AllowAnonymous();

// Liveness probe - is the application alive?
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false, // Don't run any checks
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = "alive",
            timestamp = DateTime.UtcNow
        });
    }
}).AllowAnonymous();

// Readiness probe - is the application ready to accept traffic?
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

// Detailed health check with UI response format (for debugging/monitoring)
app.MapHealthChecks("/health/details", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

// Map SignalR Hubs
app.MapHub<ChatHub>("/chatHub");
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<CallHub>("/callHub");

app.Run();


using Favi_BE.Authorization;
using Favi_BE.API.Data.Repositories;
using Favi_BE.API.HealthChecks;
using Favi_BE.API.Hubs;
using Favi_BE.API.Interfaces.Repositories;
using Favi_BE.API.Interfaces.Services;
using Favi_BE.API.Services;
using Favi_BE.Common;
using Favi_BE.Data;
using Favi_BE.Data.Repositories;
using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Repositories;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOpt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOpt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
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

        // IMPORTANT: Configure SignalR to read token from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // SignalR sends access_token as a query parameter
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
builder.Services.AddSingleton<IAuthorizationHandler, RequireAdminHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireUser", policy => policy.RequireClaim(ClaimTypes.Role, new[] { "user", "moderator", "admin" }));
    options.AddPolicy(AdminPolicies.RequireAdmin, policy =>
        policy.Requirements.Add(new RequireAdminRequirement()));
});

// Add CORS
builder.Services.AddCors(options =>
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
            .AllowAnyHeader()                // cần cho Authorization, Content-Type
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR with authentication
    });
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICollectionRepository, CollectionRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IFollowRepository, FollowRepository>();
builder.Services.AddScoped<IPostCollectionRepository, PostCollectionRepository>();
builder.Services.AddScoped<IPostMediaRepository, PostMediaRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IPostTagRepository, PostTagRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IReactionRepository, ReactionRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<ISocialLinkRepository, SocialLinkRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<IAdminActionRepository, AdminActionRepository>();
builder.Services.AddScoped<IUserModerationRepository, UserModerationRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IUserConversationRepository, UserConversationRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IEmailAccountRepository, EmailAccountRepository>();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IPrivacyGuard, PrivacyGuard>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserModerationService, UserModerationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IChatRealtimeService, ChatRealtimeService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IStoryService, StoryService>();
builder.Services.AddScoped<IBulkActionService, BulkActionService>();
builder.Services.AddScoped<IExportService, ExportService>();

// ============================================
// SYSTEM METRICS SERVICE
// ============================================
builder.Services.AddSingleton<ISystemMetricsService, SystemMetricsService>();

// ============================================
// HEALTH CHECKS CONFIGURATION
// ============================================
builder.Services.Configure<MemoryHealthCheckOptions>(options =>
{
    options.ThresholdMB = 1024;      // 1GB - Unhealthy threshold
    options.DegradedThresholdMB = 512; // 512MB - Degraded threshold
});

builder.Services.AddHealthChecks()
    // Database health check
    .AddCheck<DatabaseHealthCheck>(
        "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "postgresql", "ready"])
    // Memory health check
    .AddCheck<MemoryHealthCheck>(
        "memory",
        failureStatus: HealthStatus.Degraded,
        tags: ["system", "memory"])
    // Vector Index API health check
    .AddCheck<VectorIndexHealthCheck>(
        "vector-index-api",
        failureStatus: HealthStatus.Degraded,
        tags: ["external", "vector", "ai"])
    // Qdrant health check
    .AddCheck<QdrantHealthCheck>(
        "qdrant",
        failureStatus: HealthStatus.Degraded,
        tags: ["external", "vector", "database"])
    // Redis health check
    .AddCheck<RedisHealthCheck>(
        "redis",
        failureStatus: HealthStatus.Degraded,
        tags: ["external", "cache"]);

// Add background services
builder.Services.AddHostedService<PostCleanupService>();
builder.Services.AddHostedService<StoryExpirationService>();

// Add SignalR
builder.Services.AddSignalR();

// Configure VectorIndex options and service
builder.Services.Configure<VectorIndexOptions>(builder.Configuration.GetSection("VectorIndex"));
builder.Services.AddHttpClient<IVectorIndexService, VectorIndexService>(client =>
{
    var vectorConfig = builder.Configuration.GetSection("VectorIndex");
    var baseUrl = vectorConfig["BaseUrl"] ?? "http://vector-index-api:8080";
    var timeoutSeconds = int.Parse(vectorConfig["TimeoutSeconds"] ?? "60");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Configure NSFW detection service
builder.Services.Configure<NSFWOptions>(builder.Configuration.GetSection("NSFW"));
builder.Services.AddHttpClient<INSFWService, NSFWService>(client =>
{
    var nsfwConfig = builder.Configuration.GetSection("NSFW");
    var baseUrl = nsfwConfig["BaseUrl"] ?? "http://vector-index-api:8080";
    var timeoutSeconds = int.Parse(nsfwConfig["TimeoutSeconds"] ?? "30");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"), npgsqlOptions =>
    {
        // Enable retry on failure for transient database errors
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null
        );
    });

    // Optional: Set command timeout for long-running queries
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Check for inconsistent database state (tables exist but no migration history)
    var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
    var hasMigrationHistory = appliedMigrations.Any();
    var hasTables = false;
    try
    {
        // Try to query the Profiles table to see if tables exist
        await db.Profiles.Take(1).ToListAsync();
        hasTables = true;
    }
    catch
    {
        hasTables = false;
    }

    // If tables exist but no migration history, we need to reset
    if (hasTables && !hasMigrationHistory)
    {
        Console.WriteLine("[Migrate] Database is in inconsistent state (tables exist but no migration history).");
        Console.WriteLine("[Migrate] Dropping and recreating database...");
        await db.Database.EnsureDeletedAsync();
        Console.WriteLine("[Migrate] Database dropped. Creating fresh schema...");
    }

    // (Khuyến nghị) chờ Postgres sẵn sàng + retry vài lần
    var retries = 0;
    const int maxRetries = 10;
    while (true)
    {
        try
        {
            db.Database.Migrate(); // <- áp dụng tất cả migrations đang có
            break;
        }
        catch (Exception ex) when (retries < maxRetries)
        {
            retries++;
            Console.WriteLine($"[Migrate] retry {retries}/{maxRetries}: {ex.Message}");
            await Task.Delay(2000);
        }
    }

    // Seed data if database is empty
    await Favi_BE.API.Data.SeedData.InitializeAsync(app.Services);
}

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


using Favi_BE.API.Data.Repositories;
using Favi_BE.API.Interfaces.Repositories;
using Favi_BE.API.Interfaces.Services;
using Favi_BE.API.Services;
using Favi_BE.BuildingBlocks.Application;
using Favi_BE.BuildingBlocks.Application.Data;
using Favi_BE.BuildingBlocks.Application.Events;
using Favi_BE.BuildingBlocks.Application.Inbox;
using Favi_BE.BuildingBlocks.Application.Outbox;
using Favi_BE.BuildingBlocks.Infrastructure.Events;
using Favi_BE.BuildingBlocks.Infrastructure.ExecutionContext;
using Favi_BE.BuildingBlocks.Infrastructure.Inbox;
using Favi_BE.BuildingBlocks.Infrastructure.Outbox;
using Favi_BE.BuildingBlocks.Infrastructure.Pipeline;
using Favi_BE.Data;
using Favi_BE.Data.Repositories;
using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Repositories;
using Favi_BE.Interfaces.Services;
using Favi_BE.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Favi_BE.API.DependencyInjection;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplicationExtensions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IBuildingBlocksDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IDomainEventsAccessor, DomainEventsAccessor>();
        services.AddScoped<IDomainEventsDispatcher, DomainEventsDispatcher>();
        services.AddScoped<IDomainNotificationsMapper, DomainNotificationsMapper>();
        services.AddScoped<IOutbox, EfCoreOutbox>();
        services.AddScoped<IInbox, EfCoreInbox>();
        services.AddScoped<IExecutionContextAccessor, HttpExecutionContextAccessor>();
        services.AddHttpContextAccessor();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        });

        // Auth module
        services.AddAuthModule();

        // Engagement module
        services.AddEngagementModule();

        // Social Graph module
        services.AddSocialGraphModule();

        // Content Publishing module
        services.AddContentPublishingModule();

        // Stories module
        services.AddStoriesModule();

        // Notifications module
        services.AddNotificationsModule();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        services.AddScoped<ICollectionRepository, CollectionRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IFollowRepository, FollowRepository>();
        services.AddScoped<IPostCollectionRepository, PostCollectionRepository>();
        services.AddScoped<IPostMediaRepository, PostMediaRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IPostTagRepository, PostTagRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IReactionRepository, ReactionRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<ISocialLinkRepository, SocialLinkRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IAdminActionRepository, AdminActionRepository>();
        services.AddScoped<IUserModerationRepository, UserModerationRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IUserConversationRepository, UserConversationRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IEmailAccountRepository, EmailAccountRepository>();

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IPrivacyGuard, PrivacyGuard>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserModerationService, UserModerationService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IChatRealtimeService, ChatRealtimeService>();
        // OutboxNotificationService replaces NotificationService for write side effects.
        // Legacy NotificationService.cs is kept in codebase as rollback fallback (git revert).
        services.AddScoped<INotificationService, OutboxNotificationService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IStoryService, StoryService>();
        services.AddScoped<IBulkActionService, BulkActionService>();
        services.AddScoped<IExportService, ExportService>();

        services.AddSingleton<ISystemMetricsService, SystemMetricsService>();

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

        services.AddOpenApi();
        services.AddEndpointsApiExplorer();

        return services;
    }
}

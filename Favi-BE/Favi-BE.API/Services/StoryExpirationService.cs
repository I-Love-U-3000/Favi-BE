using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Modules.Stories.Application.Commands.ExpireStory;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Favi_BE.Services
{
    public class StoryExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StoryExpirationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

        public StoryExpirationService(IServiceProvider serviceProvider, ILogger<StoryExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StoryExpirationService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredStoriesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up expired stories.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("StoryExpirationService is stopping.");
        }

        private async Task CleanupExpiredStoriesAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var cloudinary = scope.ServiceProvider.GetRequiredService<ICloudinaryService>();

            var expiredStories = await uow.Stories.GetExpiredStoriesAsync();

            if (!expiredStories.Any())
            {
                _logger.LogInformation("No expired stories to clean up.");
                return;
            }

            _logger.LogInformation("Found {Count} expired stories to delete.", expiredStories.Count());

            foreach (var story in expiredStories)
            {
                try
                {
                    var result = await mediator.Send(new ExpireStoryCommand(story.Id), ct);

                    if (result.Success && !string.IsNullOrWhiteSpace(result.MediaPublicId))
                        await cloudinary.TryDeleteAsync(result.MediaPublicId, ct);

                    _logger.LogInformation("Successfully expired story {StoryId}.", story.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error expiring story {StoryId}.", story.Id);
                }
            }
        }
    }
}

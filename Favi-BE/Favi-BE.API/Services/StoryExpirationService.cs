using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
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
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

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
                    await CleanupExpiredStoriesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up expired stories.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("StoryExpirationService is stopping.");
        }

        private async Task CleanupExpiredStoriesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var cloudinary = scope.ServiceProvider.GetRequiredService<ICloudinaryService>();

            var expiredStories = await uow.Stories.GetExpiredStoriesAsync();

            if (!expiredStories.Any())
            {
                _logger.LogInformation("No expired stories to clean up.");
                return;
            }

            _logger.LogInformation($"Found {expiredStories.Count()} expired stories to delete.");

            foreach (var story in expiredStories)
            {
                try
                {
                    // Delete from Cloudinary
                    if (!string.IsNullOrWhiteSpace(story.MediaPublicId))
                    {
                        await cloudinary.TryDeleteAsync(story.MediaPublicId);
                    }

                    // Delete from database (cascade will delete StoryViews)
                    uow.Stories.Remove(story);
                    await uow.CompleteAsync();

                    _logger.LogInformation($"Successfully deleted expired story {story.Id}.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error deleting expired story {story.Id}.");
                }
            }

            _logger.LogInformation($"Story cleanup completed. Deleted {expiredStories.Count()} expired stories.");
        }
    }
}

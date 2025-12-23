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
    public class PostCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PostCleanupService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Run once per day

        public PostCleanupService(IServiceProvider serviceProvider, ILogger<PostCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PostCleanupService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredPostsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up expired posts.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("PostCleanupService is stopping.");
        }

        private async Task CleanupExpiredPostsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var cloudinary = scope.ServiceProvider.GetRequiredService<ICloudinaryService>();

            var now = DateTime.UtcNow;

            // Get all posts that are expired (DeletedDayExpiredAt is not null and is in the past)
            var expiredPosts = await uow.Posts.FindAsync(p =>
                p.DeletedDayExpiredAt != null && p.DeletedDayExpiredAt <= now);

            if (!expiredPosts.Any())
            {
                _logger.LogInformation("No expired posts to clean up.");
                return;
            }

            _logger.LogInformation($"Found {expiredPosts.Count()} expired posts to delete.");

            foreach (var post in expiredPosts)
            {
                try
                {
                    // Get media files to delete from Cloudinary
                    var medias = await uow.PostMedia.GetByPostIdAsync(post.Id);
                    var publicIds = medias
                        .Select(m => m.PublicId)
                        .Where(pid => !string.IsNullOrWhiteSpace(pid))
                        .Distinct()
                        .ToList();

                    // Delete from database
                    uow.PostMedia.RemoveRange(medias);
                    uow.Posts.Remove(post);

                    await uow.CompleteAsync();

                    // Clean up orphan tags
                    var orphanTags = await uow.Tags.GetTagsWithNoPostsAsync();
                    foreach (var tag in orphanTags)
                        uow.Tags.Remove(tag);

                    await uow.CompleteAsync();

                    // Delete from Cloudinary (don't fail if this errors)
                    if (publicIds.Count > 0)
                    {
                        foreach (var pid in publicIds)
                        {
                            _ = await cloudinary.TryDeleteAsync(pid);
                        }
                    }

                    _logger.LogInformation($"Successfully deleted expired post {post.Id}.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error deleting expired post {post.Id}.");
                }
            }

            _logger.LogInformation($"Cleanup completed. Deleted {expiredPosts.Count()} expired posts.");
        }
    }
}

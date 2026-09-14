using System.Diagnostics;
using System.Text.Json;
using Favi_BE.Data;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Favi_BE.API.Services;

public sealed class PostAIProcessingWorker : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceProvider _serviceProvider;
    private readonly RedisStreamOptions _redisOptions;
    private readonly VectorIndexOptions _vectorOptions;
    private readonly ILogger<PostAIProcessingWorker> _logger;

    public PostAIProcessingWorker(
        IConnectionMultiplexer redis,
        IServiceProvider serviceProvider,
        IOptions<RedisStreamOptions> redisOptions,
        IOptions<VectorIndexOptions> vectorOptions,
        ILogger<PostAIProcessingWorker> logger)
    {
        _redis = redis;
        _serviceProvider = serviceProvider;
        _redisOptions = redisOptions.Value;
        _vectorOptions = vectorOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[AI_WORKER_STARTED] PostAIProcessingWorker started on Redis stream {StreamKey}", _redisOptions.StreamKey);

        await EnsureConsumerGroupAsync();

        var db = _redis.GetDatabase();
        var streamKey = _redisOptions.StreamKey;
        var groupName = _redisOptions.ConsumerGroup;
        var consumerName = _redisOptions.ConsumerName;
        var batchSize = _vectorOptions.BulkBatchSize > 0 ? _vectorOptions.BulkBatchSize : 16;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var streamEntries = await db.StreamReadGroupAsync(
                    streamKey,
                    groupName,
                    consumerName,
                    StreamPosition.NewMessages,
                    count: batchSize
                );

                if (streamEntries.Length == 0)
                {
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                var batchId = Guid.NewGuid().ToString("N")[..8];
                var batchSw = Stopwatch.StartNew();

                _logger.LogInformation(
                    "[AI_BATCH_START] Batch {BatchId} started with {PostCount} messages on consumer {ConsumerName}",
                    batchId, streamEntries.Length, consumerName);

                await ProcessBatchAsync(batchId, streamEntries, db, streamKey, groupName, stoppingToken);

                batchSw.Stop();
                _logger.LogInformation(
                    "[AI_BATCH_COMPLETE] Batch {BatchId} processed in {TotalElapsedMs}ms",
                    batchId, batchSw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI_WORKER_ERROR] Error in PostAIProcessingWorker loop. Backing off 2000ms");
                await Task.Delay(2000, stoppingToken);
            }
        }

        _logger.LogInformation("[AI_WORKER_STOPPED] PostAIProcessingWorker stopped");
    }

    private async Task ProcessBatchAsync(
        string batchId,
        StreamEntry[] streamEntries,
        IDatabase db,
        string streamKey,
        string groupName,
        CancellationToken cancellationToken)
    {
        var postIds = new List<Guid>();
        var messageIds = new List<RedisValue>();

        foreach (var entry in streamEntries)
        {
            messageIds.Add(entry.Id);
            var postIdValue = entry.Values.FirstOrDefault(v => v.Name == "postId").Value;
            if (Guid.TryParse(postIdValue, out var guid))
            {
                postIds.Add(guid);
            }
        }

        if (postIds.Count == 0)
        {
            await db.StreamAcknowledgeAsync(streamKey, groupName, messageIds.ToArray());
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var nsfwService = scope.ServiceProvider.GetRequiredService<INSFWService>();
        var vectorService = scope.ServiceProvider.GetRequiredService<IVectorIndexService>();

        var posts = await appDb.Posts
            .Include(p => p.PostMedias)
            .Where(p => postIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        // 1. Asynchronous NSFW Evaluation
        var nsfwUpdates = 0;
        foreach (var post in posts)
        {
            if (nsfwService.IsEnabled())
            {
                var nsfwSw = Stopwatch.StartNew();
                try
                {
                    var isNsfw = await nsfwService.CheckPostAsync(post, cancellationToken);
                    nsfwSw.Stop();

                    _logger.LogInformation(
                        "[NSFW_EVALUATED] Post {PostId}: IsNSFW={IsNSFW}, ElapsedMs={ElapsedMs}ms",
                        post.Id, isNsfw, nsfwSw.ElapsedMilliseconds);

                    if (post.IsNSFW != isNsfw)
                    {
                        post.IsNSFW = isNsfw;
                        nsfwUpdates++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[NSFW_ERROR] Failed NSFW check for Post {PostId}", post.Id);
                }
            }
        }

        if (nsfwUpdates > 0)
        {
            await appDb.SaveChangesAsync(cancellationToken);
        }

        // 2. Batched Vector Indexing
        if (vectorService.IsEnabled() && posts.Count > 0)
        {
            await vectorService.IndexPostsBatchAsync(posts, cancellationToken);
        }

        // 3. Acknowledge processed messages
        await db.StreamAcknowledgeAsync(streamKey, groupName, messageIds.ToArray());
    }

    private async Task EnsureConsumerGroupAsync()
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.StreamCreateConsumerGroupAsync(
                _redisOptions.StreamKey,
                _redisOptions.ConsumerGroup,
                StreamPosition.NewMessages,
                createStream: true
            );
            _logger.LogInformation(
                "[AI_WORKER_GROUP_READY] Consumer group {GroupName} created or verified on stream {StreamKey}",
                _redisOptions.ConsumerGroup, _redisOptions.StreamKey);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // Consumer group already exists - safe to continue
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AI_WORKER_GROUP_WARN] Could not initialize consumer group. Stream may not exist yet.");
        }
    }
}

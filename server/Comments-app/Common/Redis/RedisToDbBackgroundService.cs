using CommentApp.Common.Models;
using CommentApp.Common.Services.CommentService;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace CommentApp.Common.Redis
{
    public class RedisToDbBackgroundService : BackgroundService
    {
        private readonly ILogger<RedisToDbBackgroundService> logger;
        private readonly IDatabase redisDatabase;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly BackgroundRedisOptions settings;
        private int retryCount = 0;
        public RedisToDbBackgroundService(
        ILogger<RedisToDbBackgroundService> logger,
        IDatabase redisDatabase,
        IServiceScopeFactory serviceScopeFactory)
        {
            this.logger = logger;
            this.redisDatabase = redisDatabase;
            this.serviceScopeFactory = serviceScopeFactory;
            this.settings = GetOptions();
        }
        private BackgroundRedisOptions GetOptions()
        {
            using var scope = serviceScopeFactory.CreateScope();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<BackgroundRedisOptions>>();
            return options.Value;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Redis to DB Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var commentsBatch = await GetCommentsAsync(stoppingToken);

                    if (commentsBatch.Count == 1)
                    {
                        await ProcessSingleCommentRecord(commentsBatch.First());
                        retryCount = 0;
                    }
                    else if (commentsBatch.Count > 1)
                    {
                        await ProcessBatchCommentRecords(commentsBatch);
                        retryCount = 0;
                    }
                    else
                    {
                        await Task.Delay(1000, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount > settings.MaxRetryAttempts)
                    {
                        logger.LogCritical(ex, "Max retry attempts exceeded. Background service is stopping.");
                        break;
                    }

                    var delay = CalculateExponentialBackoff(retryCount, settings.RetryDelayMilliseconds);
                    logger.LogError(ex, "Error processing messages from Redis. Retrying after {Delay} ms.", delay);
                    await Task.Delay(delay);
                }
            }

            logger.LogInformation("Redis to DB Background Service is stopping.");
        }

        private int CalculateExponentialBackoff(int retryCount, int baseDelay)
        {
            int delay = (int)(baseDelay * Math.Pow(2, retryCount));
            return Math.Min(delay, settings.MaxRetryDelayMilliseconds);
        }

        private async Task<List<Comment>> GetCommentsAsync(CancellationToken stoppingToken)
        {
            var comments = new List<Comment>();

            try
            {
                var count = await redisDatabase.ListLengthAsync(settings.RedisQueueKey);
                if (count < settings.SingleProcessingThreshold)
                {
                    var result = await redisDatabase.ListLeftPopAsync(settings.RedisQueueKey);
                    if (!result.IsNullOrEmpty)
                    {
                        var comment = DeserializeComment(result);
                        if (comment != null)
                            comments.Add(comment);
                    }
                }
                else
                {
                    for (int i = 0; i < settings.BatchSize; i++)
                    {
                        var result = await redisDatabase.ListLeftPopAsync(settings.RedisQueueKey);
                        if (result.IsNullOrEmpty)
                            continue;
                        var comment = DeserializeComment(result);
                        if (comment != null)
                            comments.Add(comment);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving comments from Redis.");
                throw;
            }

            return comments;
        }

        private Comment? DeserializeComment(RedisValue value)
        {
            try
            {
                return JsonSerializer.Deserialize<Comment>(value, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to deserialize comment: {Value}", value);
                return null;
            }
        }

        private async Task ProcessSingleCommentRecord(Comment comment)
        {
            if (comment == null)
            {
                logger.LogWarning("Attempted to process a null comment.");
                return;
            }

            using var scope = serviceScopeFactory.CreateScope();
            var commentService = scope.ServiceProvider.GetRequiredService<ICommentService>();

            try
            {
                await commentService.CreateCommentAsync(comment);
                logger.LogInformation("Processed single comment with ID: {Id}", comment.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process single comment with ID: {Id}. Re-queueing the comment.", comment.Id);
                var serialized = JsonSerializer.Serialize(comment);
                await redisDatabase.ListLeftPushAsync(settings.RedisQueueKey, serialized);
                throw;
            }
        }

        private async Task ProcessBatchCommentRecords(List<Comment> comments)
        {
            if (comments == null || comments.Count == 0)
                return;

            using var scope = serviceScopeFactory.CreateScope();
            var commentService = scope.ServiceProvider.GetRequiredService<ICommentService>();

            try
            {
                await commentService.CreateCommentBatchAsync(comments);
                logger.LogInformation("Processed comments with IDs: {Ids}", string.Join(", ", comments.Select(c => c.Id)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process batch of comments. Re-queueing messages.");
                foreach (var comment in comments)
                {
                    var serialized = JsonSerializer.Serialize(comment);
                    await redisDatabase.ListLeftPushAsync(settings.RedisQueueKey, serialized);
                }
                throw;
            }
        }
    }
}

using CommentApp.Common.Kafka.TopicCreator;
using CommentApp.Common.Models.Options;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CommentApp.Common.Kafka.Consumer
{
    public class CommentConsumer : BackgroundService
    {
        private readonly IConsumer<Null, string> consumer;
        private readonly ILogger<CommentConsumer> logger;
        private readonly IKafkaTopicCreator kafkaTopicCreator;
        private readonly IDatabase redisDatabase;
        private readonly ConsumerOptions consumerOptions;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private int retryCount = 0;
        public CommentConsumer(IConsumer<Null, string> consumer,
        ILogger<CommentConsumer> logger,
        IKafkaTopicCreator kafkaTopicCreator,
        IDatabase redisDatabase,
        IServiceScopeFactory serviceScopeFactory)
        {
            this.consumer = consumer;
            this.logger = logger;
            this.kafkaTopicCreator = kafkaTopicCreator;
            this.redisDatabase = redisDatabase;
            this.serviceScopeFactory = serviceScopeFactory;
            this.consumerOptions = GetOptions();
            this.consumer.Subscribe("comments-new");
        }
        private ConsumerOptions GetOptions()
        {
            using var scope = serviceScopeFactory.CreateScope();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<ConsumerOptions>>();
            return options.Value;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await kafkaTopicCreator.CreateTopicAsync();
            await ConsumeComment(stoppingToken);
        }

        public void StopConsuming()
        {
            consumer.Close();
        }

        private async Task ConsumeComment(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(cancellationToken);
                    var messageValue = consumeResult?.Message.Value;
                    if (string.IsNullOrEmpty(messageValue)) throw new ArgumentException($"Comment can't be processed: {messageValue}");
                    bool enqueued = await redisDatabase.ListRightPushAsync(consumerOptions.CommentConsumerQueueKey, messageValue) > 0;
                    if (enqueued)
                    {
                        consumer.Commit(consumeResult);
                        logger.LogInformation($"Enqueued comment message: {consumeResult.Message.Key}");
                    }
                    else
                        throw new Exception("Failed to enqueue message to Redis.");
                    retryCount = 0;
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Consuming cancelled.");
                    consumer.Close();
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred while consuming messages.");
                    retryCount++;
                    if (retryCount > consumerOptions.MaxRetryCount)
                    {
                        logger.LogCritical("Max retry attempts exceeded. Background service is stopping.");
                        consumer.Close();
                        break;
                    }
                    var delay = CalculateExponentialBackoff(retryCount, consumerOptions.RetryDelayMilliseconds);
                    logger.LogError("Error processing messages from Redis. Retrying after {Delay} ms.", delay);
                    await Task.Delay(delay);
                }
            }
        }
        private int CalculateExponentialBackoff(int retryCount, int baseDelay)
        {
            int delay = (int)(baseDelay * Math.Pow(2, retryCount));
            return Math.Min(delay, consumerOptions.MaxRetryDelayMilliseconds);
        }
    }
}

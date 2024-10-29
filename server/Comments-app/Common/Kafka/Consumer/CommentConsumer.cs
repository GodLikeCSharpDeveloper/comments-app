using CommentApp.Common.Kafka.TopicCreator;
using Confluent.Kafka;
using StackExchange.Redis;

namespace CommentApp.Common.Kafka.Consumer
{
    public class CommentConsumer(IConsumer<Null, string> consumer,
        ILogger<CommentConsumer> logger,
        IKafkaTopicCreator kafkaTopicCreator,
        IDatabase redisDatabase) : BackgroundService
    {
        private readonly IConsumer<Null, string> consumer = consumer;
        private readonly ILogger<CommentConsumer> logger = logger;
        private readonly IKafkaTopicCreator kafkaTopicCreator = kafkaTopicCreator;
        private readonly IDatabase redisDatabase = redisDatabase;
        private const string RedisQueueKey = "comment_queue";
        private CancellationTokenSource? cts;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await kafkaTopicCreator.CreateTopicAsync();
            cts = new CancellationTokenSource();
            _ = Task.Run(() => ConsumeComment(cts.Token));
        }

        public void StopConsuming()
        {
            if (cts != null)
            {
                cts.Cancel();
                consumer.Close();
            }
        }

        private async Task ConsumeComment(CancellationToken cancellationToken)
        {
            consumer.Subscribe("comments-new");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(cancellationToken);
                    var messageValue = consumeResult.Message.Value;
                    if (string.IsNullOrEmpty(messageValue)) throw new ArgumentException($"Comment can't be processed: {messageValue}");
                    bool enqueued = await redisDatabase.ListRightPushAsync(RedisQueueKey, messageValue) > 0;
                    if (enqueued)
                    {
                        consumer.Commit(consumeResult);
                        logger.LogInformation($"Enqueued comment message: {consumeResult.Message.Key}");
                    }
                    else
                        logger.LogWarning("Failed to enqueue message to Redis.");

                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Consuming cancelled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while consuming messages.");
            }
            finally
            {
                consumer.Close();
            }
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using CommentApp.Common.Models;
using static Confluent.Kafka.ConfigPropertyNames;
using Microsoft.Extensions.DependencyInjection;
using CommentApp.Common.Kafka.TopicCreator;
using CommentApp.Common.Services.CommentService;

namespace CommentApp.Common.Kafka.Consumer
{
    public class CommentConsumer(IConsumer<Null, string> consumer,
        ILogger<CommentConsumer> logger,
        IServiceProvider serviceProvider,
        IKafkaTopicCreator kafkaTopicCreator) : ICommentConsumer
    {
        private readonly IConsumer<Null, string> consumer = consumer;
        private readonly ILogger<CommentConsumer> logger = logger;
        private readonly IServiceProvider serviceProvider = serviceProvider;
        private readonly IKafkaTopicCreator kafkaTopicCreator = kafkaTopicCreator;
        private CancellationTokenSource? cts;

        public async Task StartConsumingAsync()
        {
            var topicName = "comments-new";
            var numPartitions = 3;
            var replicationFactor = (short)1;
            await kafkaTopicCreator.CreateTopicAsync(topicName, numPartitions, replicationFactor);
            cts = new CancellationTokenSource();
            _ = ConsumeComment(cts.Token);
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
                    var comment = JsonConvert.DeserializeObject<Comment>(messageValue);
                    if (comment == null) throw new ArgumentException($"Comment can't be processed: {comment}");
                    using (var scope = serviceProvider.CreateScope())
                    {
                        var commentService = scope.ServiceProvider.GetRequiredService<ICommentService>();
                        await commentService.CreateCommentAsync(comment);
                    }
                    logger.LogInformation($"Comment with ID {comment?.Id} processed.");
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

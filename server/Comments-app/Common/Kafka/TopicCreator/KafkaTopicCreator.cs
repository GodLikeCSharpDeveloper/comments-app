using CommentApp.Common.Models.Options;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace CommentApp.Common.Kafka.TopicCreator
{
    public class KafkaTopicCreator(string bootstrapServers,
        ILogger<KafkaTopicCreator> logger,
        List<TopicOptions> options) : IKafkaTopicCreator
    {
        private readonly string bootstrapServers = bootstrapServers;
        private readonly ILogger<KafkaTopicCreator> logger = logger;
        private readonly List<TopicOptions> options = options;
        public async Task CreateTopicAsync()
        {
            var config = new AdminClientConfig { BootstrapServers = bootstrapServers };

            using var adminClient = new AdminClientBuilder(config).Build();
            foreach (var topic in options)
            {
                var topicSpecification = new TopicSpecification
                {
                    Name = topic.TopicName,
                    NumPartitions = topic.ParticionsCount,
                    ReplicationFactor = topic.ReplicationFactor
                };

                try
                {
                    await adminClient.CreateTopicsAsync([topicSpecification]);
                    logger.LogInformation($"Topic '{topic.TopicName}' was created with {topic.ParticionsCount} partitions.");
                }
                catch (CreateTopicsException e)
                {
                    if (e.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
                    {
                        logger.LogInformation($"Topic '{topic.TopicName}' already exist.");
                    }
                    else
                    {
                        logger.LogError($"Error while creating topic: {e.Results[0].Error.Reason}");
                        throw;
                    }
                }
            }
        }
    }
}

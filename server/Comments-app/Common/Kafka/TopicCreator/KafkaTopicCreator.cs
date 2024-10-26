using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace CommentApp.Common.Kafka.TopicCreator
{
    public class KafkaTopicCreator(string bootstrapServers, ILogger<KafkaTopicCreator> logger) : IKafkaTopicCreator
    {
        private readonly string bootstrapServers = bootstrapServers;
        private readonly ILogger<KafkaTopicCreator> logger = logger;
        public async Task CreateTopicAsync(string topicName, int numPartitions, short replicationFactor)
        {
            var config = new AdminClientConfig { BootstrapServers = bootstrapServers };

            using var adminClient = new AdminClientBuilder(config).Build();

            var topicSpecification = new TopicSpecification
            {
                Name = topicName,
                NumPartitions = numPartitions,
                ReplicationFactor = replicationFactor
            };

            try
            {
                await adminClient.CreateTopicsAsync(new[] { topicSpecification });
                logger.LogInformation($"Topic '{topicName}' was created with {numPartitions} partitions.");
            }
            catch (CreateTopicsException e)
            {
                if (e.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
                {
                    logger.LogInformation($"Topic '{topicName}' already exist.");
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

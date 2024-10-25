namespace CommentApp.Common.Kafka.TopicCreator
{
    public interface IKafkaTopicCreator
    {
        public Task CreateTopicAsync(string topicName, int numPartitions, short replicationFactor);
    }
}

using CommentApp.Common.Models.Options;

namespace CommentApp.Common.Kafka.TopicCreator
{
    public interface IKafkaTopicCreator
    {
        public Task CreateTopicAsync();
    }
}

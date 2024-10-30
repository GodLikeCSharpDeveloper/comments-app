using Confluent.Kafka;
using System.Threading.Channels;

namespace CommentApp.Common.Kafka.Producer
{
    public interface IKafkaQueueService
    {
        Task EnqueueMessageAsync(Message<Null, string> message, CancellationToken cancellationToken = default);
        Channel<Message<Null, string>> MessageChannel { get; set; }
    }
}

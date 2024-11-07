using CommentApp.Common.Kafka.Producer;
using Confluent.Kafka;
using System.Threading.Channels;

public class KafkaQueueService : IKafkaQueueService
{
    public Channel<Message<Null, string>> MessageChannel { get; set; }
    public KafkaQueueService()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        MessageChannel = Channel.CreateBounded<Message<Null, string>>(options);
    }

    public async Task EnqueueMessageAsync(Message<Null, string> message, CancellationToken cancellationToken = default)
    {
        if (!await MessageChannel.Writer.WaitToWriteAsync(cancellationToken))
        {
            throw new InvalidOperationException("Cannot write to the Kafka message channel.");
        }
        await MessageChannel.Writer.WriteAsync(message, cancellationToken);
    }
}

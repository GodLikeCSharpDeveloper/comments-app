using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using System.Threading.Tasks;

public class KafkaQueueService : BackgroundService
{
    private readonly IProducer<Null, string> kafkaProducer;
    private readonly Channel<Message<Null, string>> messageChannel;
    private readonly ILogger<KafkaQueueService> logger;

    public KafkaQueueService(IProducer<Null, string> producer, ILogger<KafkaQueueService> logger)
    {
        kafkaProducer = producer;
        this.logger = logger;
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        messageChannel = Channel.CreateBounded<Message<Null, string>>(options);
    }

    public async Task EnqueueMessageAsync(Message<Null, string> message, CancellationToken cancellationToken = default)
    {
        if (!await messageChannel.Writer.WaitToWriteAsync(cancellationToken))
        {
            throw new InvalidOperationException("Cannot write to the Kafka message channel.");
        }
        await messageChannel.Writer.WriteAsync(message, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in messageChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var result = await kafkaProducer.ProduceAsync("comments-new", message, stoppingToken);
                logger.LogInformation("Message sent to Kafka. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}", result.Topic, result.Partition, result.Offset);
            }
            catch (ProduceException<Null, string> ex)
            {
                logger.LogError(ex, "Kafka produce error: {Error}", ex.Error.Reason);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while sending message to Kafka.");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        messageChannel.Writer.Complete();
        await base.StopAsync(cancellationToken);
    }
}

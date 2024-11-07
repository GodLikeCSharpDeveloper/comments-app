using CommentApp.Common.Kafka.Producer;
using Confluent.Kafka;
using System.Threading.Channels;

public class KafkaHostedService(IProducer<Null, string> producer, ILogger<KafkaHostedService> logger, IKafkaQueueService kafkaQueueService) : BackgroundService
{
    private readonly IProducer<Null, string> kafkaProducer = producer;
    private readonly Channel<Message<Null, string>> messageChannel = kafkaQueueService.MessageChannel;
    private readonly ILogger<KafkaHostedService> logger = logger;

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

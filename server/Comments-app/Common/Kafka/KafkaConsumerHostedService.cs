using CommentApp.Common.Kafka.Consumer;

public class KafkaConsumerHostedService(ICommentConsumer commentConsumer) : IHostedService
{
    private readonly ICommentConsumer commentConsumer = commentConsumer;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await commentConsumer.StartConsumingAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        commentConsumer.StopConsuming();
        return Task.CompletedTask;
    }
}

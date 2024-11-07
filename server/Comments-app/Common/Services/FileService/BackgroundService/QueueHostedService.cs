public class QueuedHostedService(IBackgroundTaskQueue taskQueue, ILogger<QueuedHostedService> logger) : BackgroundService
{
    private readonly ILogger<QueuedHostedService> logger = logger;
    private readonly IBackgroundTaskQueue taskQueue = taskQueue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Background task queue is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await taskQueue.DequeueAsync(stoppingToken);
            if (workItem == null)
            {
                await Task.Delay(1000);
                continue;
            }
            try
            {
                await workItem(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing background work item.");
            }
        }

        logger.LogInformation("Background task queue is stopping.");
    }
}

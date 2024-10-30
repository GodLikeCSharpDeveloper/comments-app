using System.Collections.Concurrent;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly ConcurrentQueue<Func<CancellationToken, Task>> workItems = new();
    private readonly SemaphoreSlim signal = new(0);

    public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        workItems.Enqueue(workItem);
        signal.Release();
    }

    public async Task<Func<CancellationToken, Task>?> DequeueAsync(CancellationToken cancellationToken)
    {
        await signal.WaitAsync(cancellationToken);
        workItems.TryDequeue(out var workItem);
        return workItem;
    }
}

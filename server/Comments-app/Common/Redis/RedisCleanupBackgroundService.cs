using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

public class RedisCleanupBackgroundService(IConnectionMultiplexer redis)
{
    private readonly IConnectionMultiplexer _redis = redis;

    public async Task StopAsync()
    {
        var db = _redis.GetDatabase();
        await db.ExecuteAsync("FLUSHDB");
    }
}

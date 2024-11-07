using StackExchange.Redis;
namespace CommentApp.Common.Redis
{
    public class RedisCleanupService(IConnectionMultiplexer redis)
    {
        private readonly IConnectionMultiplexer _redis = redis;

        public async Task StopAsync()
        {
            var db = _redis.GetDatabase();
            await db.ExecuteAsync("FLUSHDB");
        }
    }
}
using StackExchange.Redis;

public class RedisOptions
{
    public string ConnectionString { get; set; }
    public int SyncTimeout { get; set; }
    public int ConnectTimeout { get; set; }
    public int KeepAlive { get; set; }
    public bool AbortOnConnectFail { get; set; }
    public int ReconnectRetryInterval { get; set; }
    public bool AllowAdmin { get; set; }
    public int DefaultDatabase { get; set; }
}
public class BackgroundRedisOptions
{
    public RedisKey RedisQueueKey { get => new RedisKey(RedisKeyValue); }
    public string RedisKeyValue { get; set; }
    public int BatchSize { get; set; }
    public int SingleProcessingThreshold { get; set; }
    public int RetryDelayMilliseconds { get; set; }
    public int MaxRetryAttempts { get; set; }
}

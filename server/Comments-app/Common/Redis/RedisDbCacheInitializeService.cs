using CommentApp.Common.Services.CommentService;
using CommentApp.Common.Services.UserService;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CommentApp.Common.Redis
{
    public class RedisDbCacheInitializeService(
        ILogger<RedisToDbBackgroundService> logger,
        IDatabase redisDatabase,
        IServiceScopeFactory serviceScopeFactory)
    {
        private readonly ILogger<RedisToDbBackgroundService> logger = logger;
        private readonly IDatabase redisDatabase = redisDatabase;
        private readonly IServiceScopeFactory serviceScopeFactory = serviceScopeFactory;
        public async Task InitializeCache()
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var users = await userService.GetUsersAsync();
                var tasks = users.Select(user =>
                {
                    var cacheKey = $"user:{user.Id}";
                    var userData = JsonConvert.SerializeObject(user);
                    return redisDatabase.StringSetAsync(cacheKey, userData);
                });
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while initializing cache");
            }
        }
    }
}

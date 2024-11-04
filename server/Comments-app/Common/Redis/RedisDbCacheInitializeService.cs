using CommentApp.Common.Services.CommentService;
using CommentApp.Common.Services.UserService;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CommentApp.Common.Redis
{
    public class RedisDbCacheInitializeService(
        ILogger<RedisToDbBackgroundService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IRedisUserCacheService cacheService)
    {
        private readonly ILogger<RedisToDbBackgroundService> logger = logger;
        private readonly IServiceScopeFactory serviceScopeFactory = serviceScopeFactory;
        private readonly IRedisUserCacheService cacheService = cacheService;
        public async Task InitializeCache()
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var users = await userService.GetUsersAsync();
                var tasks = users.Select(cacheService.AddUserToCache);
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while initializing cache");
            }
        }
    }
}

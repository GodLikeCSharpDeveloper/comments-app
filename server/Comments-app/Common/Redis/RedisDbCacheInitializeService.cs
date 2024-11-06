using CommentApp.Common.Models;
using CommentApp.Common.Services.CommentService;
using CommentApp.Common.Services.UserService;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Threading;

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
                var users = await userService.GetUsersAsync() ?? Enumerable.Empty<User>();

                if (users.Any())
                {
                    logger.LogWarning("No users found to initialize the cache.");
                }

                logger.LogInformation("Starting cache initialization with {UserCount} users.", users.Count());

                var semaphore = new SemaphoreSlim(20);
                var tasks = users.Select(async user =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        logger.LogDebug("Adding user {UserId} to cache.", user.Id);
                        await cacheService.AddUserToCache(user);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error adding user {UserId} to cache.", user.Id);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);

                logger.LogInformation("Cache initialization completed successfully.");
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Cache initialization was canceled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while initializing cache");
            }
        }
    }
}

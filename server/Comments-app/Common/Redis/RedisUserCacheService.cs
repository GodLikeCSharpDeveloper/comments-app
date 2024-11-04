using CommentApp.Common.Models;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace CommentApp.Common.Redis
{
    public class RedisUserCacheService(IDatabase database) : IRedisUserCacheService
    {
        private readonly IDatabase database = database;
        public async Task<User?> GetUserFromCache(string userEmail)
        {
            var cacheKey = $"user:{userEmail}";
            var userData = await database.StringGetAsync(cacheKey);
            if (!userData.HasValue)
                return null;
            else
                return JsonConvert.DeserializeObject<User>(userData);
        }
        public async Task AddUserToCache(User user)
        {
            var cacheKey = $"user:{user.Email}";
            var userData = JsonConvert.SerializeObject(user);
            await database.StringSetAsync(cacheKey, userData);
        }
    }
}

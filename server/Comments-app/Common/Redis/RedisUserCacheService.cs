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
            if (user.Id == 0)
                return;
            user.Comments.ForEach(com => { com.UserId = user.Id; com.User = null; });
            var cacheKey = $"user:{user.Email}";
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            var userData = JsonConvert.SerializeObject(user, settings);
            await database.StringSetAsync(cacheKey, userData);
        }
    }
}

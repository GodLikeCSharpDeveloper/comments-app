using CommentApp.Common.Models;
using Newtonsoft.Json;

namespace CommentApp.Common.Redis
{
    public interface IRedisUserCacheService
    {
        Task<User?> GetUserFromCache(string userEmail);
        Task AddUserToCache(User user);
    }
}

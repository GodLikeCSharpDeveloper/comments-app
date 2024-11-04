using CommentApp.Common.Models;
using CommentApp.Common.Redis;
using CommentApp.Common.Repositories.UserRepository;

namespace CommentApp.Common.Services.UserService
{
    public class UserService(IUserRepository userRepository, IRedisUserCacheService cacheService) : IUserService
    {
        private readonly IUserRepository userRepository = userRepository;
        private readonly IRedisUserCacheService cacheService = cacheService;
        public async Task<List<User>> GetUsersAsync()
        {
            return await userRepository.GetUsersAsync();
        }
        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await userRepository.GetUserByIdAsync(id);
        }
        public async Task<User?> GetUserByEmail(string email)
        {
            return await userRepository.GetUserByEmail(email);
        }
        public async Task CreateUserAsync(string userName, string email, string? homePage)
        {
            var user = new User { UserName = userName, Email = email, HomePage = homePage };
            await userRepository.AddUserAsync(user);
            await cacheService.AddUserToCache(user);
            await userRepository.SaveChangesAsync();
        }
        public async Task CreateOrUpdateUserBatchAsync(List<User> users)
        {
            var usersToUpdate = new List<User>();
            var usersToAdd = new List<User>();
            foreach (var user in users)
            {
                var existingUser = await cacheService.GetUserFromCache(user.Email);
                if (existingUser != null)
                {
                    existingUser.Comments.AddRange(user.Comments);
                    usersToUpdate.Add(existingUser);
                }
                else
                    usersToAdd.Add(user);
            }
            await userRepository.CreateUserBatchAsync(usersToAdd);
            await userRepository.UpdateUserBatchAsync(usersToUpdate);
            await userRepository.SaveChangesAsync();
        }
    }
}

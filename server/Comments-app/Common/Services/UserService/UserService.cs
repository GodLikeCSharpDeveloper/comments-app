using CommentApp.Common.Models;
using CommentApp.Common.Redis;
using CommentApp.Common.Repositories.CommentRepository;
using CommentApp.Common.Repositories.UserRepository;

namespace CommentApp.Common.Services.UserService
{
    public class UserService(IUserRepository userRepository, IRedisUserCacheService cacheService, ICommentRepository commentRepository) : IUserService
    {
        private readonly IUserRepository userRepository = userRepository;
        private readonly IRedisUserCacheService cacheService = cacheService;
        private readonly ICommentRepository commentRepository = commentRepository;
        public async Task<List<User>> GetUsersAsync()
        {
            var users = await userRepository.GetUsersAsync();
            users.ForEach(user => user.Comments.ForEach(comment => { comment.UserId = user.Id; comment.User = null; }));
            return users;
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
            if (usersToAdd.Count > 0)
                await userRepository.CreateUserBatchAsync(usersToAdd);
            var comments = ConvertUserToComments(usersToUpdate);
            if (comments.Count > 0)
                await commentRepository.CreateCommentBatchAsync(comments);
            await userRepository.SaveChangesAsync();
            usersToAdd.ForEach(async user => await cacheService.AddUserToCache(user));
        }
        private List<Comment> ConvertUserToComments(List<User> users)
        {
            return users.Select(user =>
            {
                var comments = user.Comments;
                comments.ForEach(com => com.UserId = user.Id);
                return comments;
            }).SelectMany(c => c).ToList();
        }
    }
}

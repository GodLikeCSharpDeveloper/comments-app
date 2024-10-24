using CommentApp.Common.Models;
using CommentApp.Common.Repositories.UserRepository;

namespace CommentApp.Common.Services.UserService
{
    public class UserService(IUserRepository userRepository) : IUserService
    {
        private readonly IUserRepository userRepository = userRepository;

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await userRepository.GetUserByIdAsync(id);
        }

        public async Task CreateUserAsync(string userName, string email, string? homePage)
        {
            var user = new User(userName, email) { HomePage = homePage };
            await userRepository.AddUserAsync(user);
            await userRepository.SaveChangesAsync();
        }
    }
}

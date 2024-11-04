using CommentApp.Common.Models;

namespace CommentApp.Common.Services.UserService
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByEmail(string email);
        Task<List<User>> GetUsersAsync();
        Task CreateUserAsync(string userName, string email, string? homePage);
        Task CreateOrUpdateUserBatchAsync(List<User> users);
    }

}

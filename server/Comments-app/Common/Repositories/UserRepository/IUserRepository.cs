using CommentApp.Common.Models;

namespace CommentApp.Common.Repositories.UserRepository
{
    public interface IUserRepository
    {
        Task<List<User>> GetUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByEmail(string email);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task AddUserAsync(User user);
        Task CreateUserBatchAsync(List<User> users);
        Task UpdateUserBatchAsync(List<User> users);
        Task SaveChangesAsync();
    }
}

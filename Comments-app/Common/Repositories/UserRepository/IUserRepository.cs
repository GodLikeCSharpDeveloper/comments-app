using CommentApp.Common.Models;

namespace CommentApp.Common.Repositories.UserRepository
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task AddUserAsync(User user);
        Task SaveChangesAsync();
    }
}

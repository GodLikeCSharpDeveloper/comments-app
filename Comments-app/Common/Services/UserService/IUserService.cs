using CommentApp.Common.Models;

namespace CommentApp.Common.Services.UserService
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(int id);
        Task CreateUserAsync(string userName, string email, string? homePage);
    }

}

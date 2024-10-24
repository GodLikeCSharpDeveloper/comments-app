using CommentApp.Common.Data;
using CommentApp.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace CommentApp.Common.Repositories.UserRepository
{
    public class UserRepository(CommentsAppDbContext context) : IUserRepository
    {
        private readonly CommentsAppDbContext context = context;

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await context.Users
                .Include(u => u.Comments)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await context.Users
                .Include(u => u.Comments)
                .ToListAsync();
        }

        public async Task AddUserAsync(User user)
        {
            await context.Users.AddAsync(user);
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }
    }

}

using CommentApp.Common.Data;
using CommentApp.Common.Models;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace CommentApp.Common.Repositories.UserRepository
{
    public class UserRepository(CommentsAppDbContext context) : IUserRepository
    {
        private readonly CommentsAppDbContext context = context;

        public async Task<List<User>> GetUsersAsync()
        {
            return await context.Users.Include(u => u.Comments).ToListAsync();
        }
        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await context.Users
                .Include(u => u.Comments)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            return await context.Users.FirstOrDefaultAsync(u => u.Email == email);
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

        public async Task CreateUserBatchAsync(List<User> users)
        {
            var strategy = context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var bulkConfig = new BulkConfig { IncludeGraph = true };
                    await context.BulkInsertAsync(users, bulkConfig);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task UpdateUserBatchAsync(List<User> users)
        {
            var bulkConfig = new BulkConfig { IncludeGraph = true };
            await context.BulkUpdateAsync(users, bulkConfig);
        }
    }
}

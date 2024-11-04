using CommentApp.Common.Data;
using CommentApp.Common.Models;
using CommentApp.Common.Models.DTOs;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace CommentApp.Common.Repositories.CommentRepository
{
    public class CommentRepository(CommentsAppDbContext context) : ICommentRepository
    {
        private readonly CommentsAppDbContext dbContext = context;

        public async Task<Comment?> GetCommentByIdAsync(int id)
        {
            return await dbContext.Comments
                .Include(c => c.User)
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
        public IQueryable<Comment> GetAllCommentsQuery()
        {
            return dbContext.Comments.Include(c => c.User).AsNoTracking();
        }
        public async Task<IEnumerable<Comment>> GetCommentsByUserIdAsync(int userId)
        {
            return await dbContext.Comments
                .Where(c => c.UserId == userId)
                .Include(c => c.Replies)
                .ToListAsync();
        }
        public async Task CreateCommentBatchAsync(List<Comment> comments)
        {
            await dbContext.BulkInsertOrUpdateAsync(comments);
        }
        public async Task AddCommentAsync(Comment comment)
        {
            await dbContext.Comments.AddAsync(comment);
        }

        public async Task SaveChangesAsync()
        {
            await dbContext.SaveChangesAsync();
        }
    }
}

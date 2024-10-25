using CommentApp.Common.Data;
using CommentApp.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace CommentApp.Common.Repositories.CommentRepository
{
    public class CommentRepository(CommentsAppDbContext context) : ICommentRepository
    {
        private readonly CommentsAppDbContext context = context;

        public async Task<Comment?> GetCommentByIdAsync(int id)
        {
            return await context.Comments
                .Include(c => c.User)
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Comment>> GetCommentsByUserIdAsync(int userId)
        {
            return await context.Comments
                .Where(c => c.UserId == userId)
                .Include(c => c.Replies)
                .ToListAsync();
        }

        public async Task AddCommentAsync(Comment comment)
        {
            await context.Comments.AddAsync(comment);
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }
    }
}

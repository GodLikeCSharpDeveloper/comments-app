using CommentApp.Common.Models;

namespace CommentApp.Common.Repositories.CommentRepository
{
    public interface ICommentRepository
    {
        Task<Comment?> GetCommentByIdAsync(int id);
        Task<IEnumerable<Comment>> GetCommentsByUserIdAsync(int userId);
        Task AddCommentAsync(Comment comment);
        Task SaveChangesAsync();
    }
}

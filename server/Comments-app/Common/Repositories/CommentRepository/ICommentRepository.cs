using CommentApp.Common.Models;

namespace CommentApp.Common.Repositories.CommentRepository
{
    public interface ICommentRepository
    {
        Task<Comment?> GetCommentByIdAsync(int id);
        Task<IEnumerable<Comment>> GetCommentsByUserIdAsync(int userId);
        Task<int> GetLastAddedCommentForUser(string email);
        IQueryable<Comment> GetAllParrentCommentsQuery();
        Task CreateCommentBatchAsync(List<Comment> comments);
        Task AddCommentAsync(Comment comment);
        Task<List<Comment>> GetAllCommentsAsync();
        Task SaveChangesAsync();
    }
}

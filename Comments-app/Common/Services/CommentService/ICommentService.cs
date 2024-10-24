using CommentApp.Common.Models;

namespace CommentApp.Common.Services.CommentService
{
    public interface ICommentService
    {
        Task<Comment?> GetCommentByIdAsync(int id);
        Task CreateCommentAsync(Comment comment);
    }
}

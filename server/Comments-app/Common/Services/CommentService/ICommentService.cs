using CommentApp.Common.Models;
using CommentApp.Common.Models.DTOs;

namespace CommentApp.Common.Services.CommentService
{
    public interface ICommentService
    {
        Task<Comment?> GetCommentByIdAsync(int id);
        Task CreateCommentAsync(Comment comment);
        Task<List<Comment>> GetCommentsByQueryAsync(CommentQueryParameters queryParameters);
        Task CreateCommentBatchAsync(List<Comment> comments);
        Task<List<Comment>> GetAllCommentsAsync();
        Task<int> CountAllComments();
    }
}

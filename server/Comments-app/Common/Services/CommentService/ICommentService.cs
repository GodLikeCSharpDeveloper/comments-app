using CommentApp.Common.Models;
using CommentApp.Common.Models.DTOs;

namespace CommentApp.Common.Services.CommentService
{
    public interface ICommentService
    {
        Task<GetCommentDto?> GetCommentByIdAsync(int id);
        Task CreateCommentAsync(Comment comment);
        Task<List<GetCommentDto>> GetCommentsByQueryAsync(CommentQueryParameters queryParameters);
        Task CreateCommentBatchAsync(List<Comment> comments);
        Task<List<GetCommentDto>> GetAllCommentsAsync();
        Task<int> CountAllComments();
        Task<int> GetLastAddedCommentForUser(string email);
    }
}

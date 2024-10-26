using CommentApp.Common.Models;
using CommentApp.Common.Repositories.CommentRepository;

namespace CommentApp.Common.Services.CommentService
{
    public class CommentService(ICommentRepository commentRepository) : ICommentService
    {
        private readonly ICommentRepository commentRepository = commentRepository;

        public async Task<Comment?> GetCommentByIdAsync(int id)
        {
            return await commentRepository.GetCommentByIdAsync(id);
        }

        public async Task CreateCommentAsync(Comment comment)
        {
            await commentRepository.AddCommentAsync(comment);
            await commentRepository.SaveChangesAsync();
        }
        public async Task CreateCommentBatchAsync(List<Comment> comments)
        {
            await commentRepository.CreateCommentBatchAsync(comments);
            await commentRepository.SaveChangesAsync();
        }
    }
}

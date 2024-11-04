using CommentApp.Common.Models;
using CommentApp.Common.Redis;
using CommentApp.Common.Repositories.CommentRepository;
using CommentApp.Common.Services.UserService;

namespace CommentApp.Common.Services.CommentService
{
    public class CommentService(ICommentRepository commentRepository, IUserService userService, IRedisUserCacheService cacheService) : ICommentService
    {
        private readonly IUserService userService = userService;
        private readonly ICommentRepository commentRepository = commentRepository;
        private readonly IRedisUserCacheService cacheService = cacheService;

        public async Task<Comment?> GetCommentByIdAsync(int id)
        {
            return await commentRepository.GetCommentByIdAsync(id);
        }

        public async Task CreateCommentAsync(Comment comment)
        {
            ArgumentNullException.ThrowIfNull(comment);
            var userFromDb = userService.GetUserByEmail(comment.User.Email);
            if (userFromDb != null)
            {
                comment.User = null;
                comment.UserId = userFromDb.Id;
            }
            else
            {
                await cacheService.AddUserToCache(comment.User);
            }
            await commentRepository.AddCommentAsync(comment);
            await commentRepository.SaveChangesAsync();
        }
        public async Task CreateCommentBatchAsync(List<Comment> comments)
        {
            await userService.CreateOrUpdateUserBatchAsync(ConvertCommentsToUsers(comments));
            await commentRepository.SaveChangesAsync();
        }
        private List<User> ConvertCommentsToUsers(List<Comment> comments)
        {
            return comments.Select(c =>
            {
                var user = c.User;
                user.Comments ??= [];
                user.Comments.Add(c);
                return user;
            }).GroupBy(u => u.Email).Select(gr =>
            {
                var firstUser = gr.FirstOrDefault();
                return new User()
                {
                    Email = gr.Key,
                    Comments = gr.SelectMany(u => u.Comments).ToList(),
                    HomePage = firstUser.HomePage,
                    Id = firstUser.Id,
                    UserName = firstUser.UserName
                };
            }).ToList();
        }
    }
}

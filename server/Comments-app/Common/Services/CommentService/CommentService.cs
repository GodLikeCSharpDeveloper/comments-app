using CommentApp.Common.Models;
using CommentApp.Common.Models.DTOs;
using CommentApp.Common.Redis;
using CommentApp.Common.Repositories.CommentRepository;
using CommentApp.Common.Services.UserService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

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
        public async Task<List<Comment>> GetAllCommentsAsync()
        {
            return await commentRepository.GetAllCommentsAsync();
        }
        public async Task<int> GetLastAddedCommentForUser(string email)
        {
            return await commentRepository.GetLastAddedCommentForUser(email);
        }
        public async Task<List<Comment>> GetCommentsByQueryAsync(CommentQueryParameters queryParameters)
        {
            var query = commentRepository.GetAllParrentCommentsQuery();
            var validSortProperties = new[] { "User.UserName", "User.Email" };
            foreach (var property in validSortProperties)
            {
                if (property.Contains(queryParameters.SortBy, StringComparison.InvariantCultureIgnoreCase))
                    queryParameters.SortBy = property;
            }
            if (queryParameters.SortBy == "undefined")
                queryParameters.SortBy = "User.UserName";
            var sortDirection = queryParameters.SortDirection?.ToLower() == "desc" ? "desc" : "asc";
            query = query.OrderBy($"{queryParameters.SortBy} {sortDirection}");
            query = query.Skip((queryParameters.PageNumber) * queryParameters.PageSize).Take(queryParameters.PageSize);
            return await query.ToListAsync();
        }
        public async Task<int> CountAllComments()
        {
            return await commentRepository.GetAllParrentCommentsQuery().CountAsync();
        }
        public async Task CreateCommentAsync(Comment comment)
        {
            ArgumentNullException.ThrowIfNull(comment);
            var userFromDb = await cacheService.GetUserFromCache(comment.User.Email);
            if (userFromDb != null)
            {
                comment.User = null;
                comment.UserId = userFromDb.Id;
                await commentRepository.AddCommentAsync(comment);
                await commentRepository.SaveChangesAsync();
            }
            else
            {
                await commentRepository.AddCommentAsync(comment);
                await commentRepository.SaveChangesAsync();
                await cacheService.AddUserToCache(comment.User);
            }
        }
        public async Task CreateCommentBatchAsync(List<Comment> comments)
        {
            await userService.CreateOrUpdateUserBatchAsync(ConvertCommentsToUsers(comments));
            await commentRepository.SaveChangesAsync();
        }
        private List<User> ConvertCommentsToUsers(List<Comment> comments)
        {
            if (comments.Any(com => com.User == null))
                return [];
            var resultUsers = comments.Select(c =>
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
            resultUsers.ForEach(user => user.Comments.ForEach(comment => { comment.UserId = user.Id; comment.User = null; }));
            return resultUsers;
        }
    }
}

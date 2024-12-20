﻿using CommentApp.Common.Data;
using CommentApp.Common.Models;
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
        public async Task<int> GetLastAddedCommentForUser(string email)
        {
            var latestCommentId = await dbContext.Comments
            .Where(c => c.User.Email == email)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
            return latestCommentId;
        }
        public async Task<List<Comment>> GetAllCommentsAsync()
        {
            var comments = await dbContext.Comments
                .Where(c => c.ParentCommentId == null)
                .Include(c => c.User)
                .ToListAsync();

            foreach (var comment in comments)
            {
                await LoadRepliesRecursively(comment);
            }

            return comments;
        }
        private async Task LoadRepliesRecursively(Comment comment)
        {
            await dbContext.Entry(comment)
                .Collection(c => c.Replies)
                .Query()
                .Include(c => c.User)
                .LoadAsync();

            foreach (var reply in comment.Replies)
            {
                await LoadRepliesRecursively(reply);
            }
        }

        public IQueryable<Comment> GetAllParentCommentsQuery()
        {
            return dbContext.Comments.Include(c => c.User).Where(p => p.ParentCommentId == null).AsNoTracking();
        }
        public async Task<IEnumerable<Comment>> GetCommentsByUserIdAsync(int userId)
        {
            return await dbContext.Comments
                .Where(c => c.UserId == userId)
                .Include(c => c.Replies)
                .AsNoTracking()
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

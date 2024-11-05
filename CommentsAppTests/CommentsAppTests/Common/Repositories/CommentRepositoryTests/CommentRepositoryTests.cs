using CommentApp.Common.Data;
using CommentApp.Common.Models;
using CommentApp.Common.Repositories.CommentRepository;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommentsAppTests.Common.Repositories.CommentRepositoryTests
{
    [TestFixture]
    public class CommentRepositoryTests
    {
        private CommentsAppDbContext dbContext;
        private CommentRepository commentRepository;

        [SetUp]
        public void Setup()
        {
            Batteries.Init();

            var options = new DbContextOptionsBuilder<CommentsAppDbContext>()
                .UseSqlite("Filename=:memory:")
                .Options;

            dbContext = new CommentsAppDbContext(options);
            dbContext.Database.OpenConnection();
            dbContext.Database.EnsureCreated();
            dbContext.Users.Add(new User { Id = 1, UserName = "Test User", Email = "testEmail@mail.com" });
            commentRepository = new CommentRepository(dbContext);
            dbContext.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            dbContext.Database.CloseConnection();
            dbContext.Dispose();
        }

        [Test]
        public async Task GetCommentByIdAsync_ReturnsComment_WhenCommentExists()
        {
            // Arrange
            var user = new User { Id = 1, UserName = "Test User" };
            var comment = new Comment
            {
                Id = 1,
                Text = "Test comment",
                UserId = user.Id
            };

            await dbContext.Comments.AddAsync(comment);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await commentRepository.GetCommentByIdAsync(comment.Id);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Id, Is.EqualTo(comment.Id));
                Assert.That(result.Text, Is.EqualTo(comment.Text));
                Assert.That(result.User, Is.Not.Null);
                Assert.That(result.User.UserName, Is.EqualTo(user.UserName));
            });

        }

        [Test]
        public async Task GetCommentByIdAsync_ReturnsNull_WhenCommentDoesNotExist()
        {
            // Act
            var result = await commentRepository.GetCommentByIdAsync(999);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetCommentsByUserIdAsync_ReturnsCommentsForUser()
        {
            // Arrange
            var user = new User { Id = 1, UserName = "Test User" };
            var comments = new List<Comment>
            {
                new Comment { Id = 1, Text = "Comment 1", UserId = user.Id },
                new Comment { Id = 2, Text = "Comment 2", UserId = user.Id }
            };

            await dbContext.Comments.AddRangeAsync(comments);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await commentRepository.GetCommentsByUserIdAsync(user.Id);

            // Assert
            Assert.That(result, Is.Not.Null);
            var commentList = result.ToList();
            Assert.That(commentList.Count, Is.EqualTo(2));
            Assert.That(commentList.All(c => c.UserId == user.Id), Is.True);
        }

        [Test]
        public async Task AddCommentAsync_AddsCommentToDatabase()
        {
            // Arrange
            var comment = new Comment { Id = 1, Text = "New Comment", UserId = 1 };

            // Act
            await commentRepository.AddCommentAsync(comment);
            await commentRepository.SaveChangesAsync();

            // Assert
            var result = await dbContext.Comments.FindAsync(comment.Id);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Text, Is.EqualTo(comment.Text));
        }

        [Test]
        public async Task CreateCommentBatchAsync_AddsCommentsToDatabase()
        {
            // Arrange
            var comments = new List<Comment>
            {
                new Comment { Id = 2, Text = "Comment 1", UserId = 1 },
                new Comment { Id = 3, Text = "Comment 2", UserId = 1 }
            };

            // Act
            await commentRepository.CreateCommentBatchAsync(comments);
            await commentRepository.SaveChangesAsync();

            // Assert
            var result = await dbContext.Comments.ToListAsync();
            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Count.EqualTo(2));
                Assert.That(result.Any(c => c.Text == "Comment 1"), Is.True);
                Assert.That(result.Any(c => c.Text == "Comment 2"), Is.True);
            });
        }
    }
}

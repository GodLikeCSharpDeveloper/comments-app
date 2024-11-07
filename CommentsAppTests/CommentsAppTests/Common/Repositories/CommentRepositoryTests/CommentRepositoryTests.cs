using CommentApp.Common.Data;
using CommentApp.Common.Models;
using CommentApp.Common.Repositories.CommentRepository;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

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
            Assert.That(result, Is.Null);
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
        [Test]
        public async Task GetLastAddedCommentForEmail_ReturnsIdOfLastCommentAdded()
        {
            //Arrange
            var comment = new Comment() { Text = "Comment 1" };
            var user = new User { UserName = "Test User", Email = "testEmail2@mail.com", Comments = [comment] };

            //Act
            await dbContext.Users.AddAsync(user);
            await commentRepository.SaveChangesAsync();
            var result = await commentRepository.GetLastAddedCommentForUser("testEmail2@mail.com");

            //Assert
            Assert.That(result, Is.EqualTo(1));
        }
        [Test]
        public async Task GetCommentByIdAsync_WithExistingId_ReturnsCommentWithUserAndReplies()
        {
            // Arrange
            var expectedComment = new Comment { UserId = 1, Text = "testText" };


            // Act
            await commentRepository.AddCommentAsync(expectedComment);
            await commentRepository.SaveChangesAsync();
            var result = await commentRepository.GetCommentByIdAsync(1);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(expectedComment));
        }
        [Test]
        public async Task GetAllCommentsAsync_ReturnsAllParentCommentsWithUserAndRepliesLoaded()
        {
            // Arrange
            var parentComment = new Comment { Text = "commentTestParrent", UserId = 1 };
            var childComment = new Comment { UserId = 1, ParentCommentId = 1, Text = "testcomment" };

            // Act
            await commentRepository.AddCommentAsync(parentComment);
            await commentRepository.SaveChangesAsync();
            await commentRepository.AddCommentAsync(childComment);
            await commentRepository.SaveChangesAsync();
            var result = await commentRepository.GetAllCommentsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.First().Replies, Has.Count.EqualTo(parentComment.Replies.Count));
        }
        [Test]
        public async Task GetAllParentCommentsQuery_ReturnsQueryableOfParentComments()
        {
            // Arrange
            var comments = new List<Comment> { new Comment { ParentCommentId = null, Text = "texttest", UserId = 1 } };


            // Act
            await commentRepository.CreateCommentBatchAsync(comments);
            await commentRepository.SaveChangesAsync();
            var result = commentRepository.GetAllParentCommentsQuery();

            // Assert
            Assert.That(result, Is.InstanceOf<IQueryable<Comment>>());
            Assert.That(result.Count(), Is.EqualTo(1));
        }
        [Test]
        public async Task GetCommentsByUserIdAsync_WithExistingUserId_ReturnsUserCommentsWithReplies()
        {
            // Arrange
            var parentComment = new Comment { Text = "commentTestParrent", UserId = 1 };
            var childComment = new Comment { UserId = 1, ParentCommentId = 1, Text = "testcomment" };

            // Act
            await commentRepository.AddCommentAsync(parentComment);
            await commentRepository.SaveChangesAsync();
            await commentRepository.AddCommentAsync(childComment);
            await commentRepository.SaveChangesAsync();
            var result = await commentRepository.GetCommentsByUserIdAsync(1);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
        }
        [Test]
        public async Task CreateCommentBatchAsync_WithComments_AddsAndSavesChanges()
        {
            // Arrange
            var comments = new List<Comment> { new Comment { Text = "testText1", UserId = 1 }, new Comment { Text = "testText2", UserId = 1 } };

            // Act
            await commentRepository.CreateCommentBatchAsync(comments);
            await commentRepository.SaveChangesAsync();
            var result = await commentRepository.GetAllCommentsAsync();

            // Assert
            Assert.That(comments.Any(com => result.Any(r => r.Text == com.Text)), Is.True);
        }

        [Test]
        public async Task AddCommentAsync_WithComment_AddsCommentToContext()
        {
            // Arrange
            var comment = new Comment { Text = "testText", UserId = 1 };

            // Act
            await commentRepository.AddCommentAsync(comment);
            await commentRepository.SaveChangesAsync();
            var result = await commentRepository.GetCommentByIdAsync(1);

            // Assert
            Assert.That(result, Is.EqualTo(comment));
        }

        [Test]
        public async Task SaveChangesAsync_CallsDbContextSaveChanges()
        {
            // Arrange
            var comment = new Comment() { Text = "testText", UserId = 1 };

            // Act
            await commentRepository.AddCommentAsync(comment);
            await commentRepository.SaveChangesAsync();

            // Assert
            Assert.That(comment.Id, Is.EqualTo(1));
        }
    }
}

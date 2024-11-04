using CommentApp.Common.Models;
using CommentApp.Common.Redis;
using CommentApp.Common.Repositories.CommentRepository;
using CommentApp.Common.Services.CommentService;
using CommentApp.Common.Services.UserService;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommentsAppTests.Common.Services.CommentServiceTests
{
    [TestFixture]
    public class CommentServiceTest
    {
        private Mock<ICommentRepository> mockCommentRepository;
        private Mock<IUserService> mockUserService;
        private CommentService commentService;
        private Mock<IRedisUserCacheService> mockCacheService;

        [SetUp]
        public void Setup()
        {
            mockCommentRepository = new Mock<ICommentRepository>();
            mockUserService = new Mock<IUserService>();
            mockCacheService = new Mock<IRedisUserCacheService>();
            commentService = new CommentService(mockCommentRepository.Object, mockUserService.Object, mockCacheService.Object);
        }
        [Test]
        public async Task GetCommentByIdAsync_ReturnsComment_WhenCommentExists()
        {
            // Arrange
            var expectedComment = new Comment { Text = "Test comment" };
            mockCommentRepository
                .Setup(repo => repo.GetCommentByIdAsync(1))
                .ReturnsAsync(expectedComment);

            // Act
            var result = await commentService.GetCommentByIdAsync(1);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(expectedComment));
            mockCommentRepository.Verify(repo => repo.GetCommentByIdAsync(1), Times.Once);
        }
        [Test]
        public async Task GetCommentByIdAsync_ReturnsNull_WhenCommentDoesNotExist()
        {
            // Arrange
            mockCommentRepository
                .Setup(repo => repo.GetCommentByIdAsync(1))
                .ReturnsAsync((Comment)null);

            // Act
            var result = await commentService.GetCommentByIdAsync(1);

            // Assert
            Assert.That(result, Is.Null);
            mockCommentRepository.Verify(repo => repo.GetCommentByIdAsync(1), Times.Once);
        }
        [Test]
        public async Task CreateCommentAsync_AddsCommentAndSavesChanges()
        {
            // Arrange
            var newComment = new Comment { Text = "New comment", User = new User() { Email = "testEmail@mail.com" } };

            // Act
            await commentService.CreateCommentAsync(newComment);

            // Assert
            mockCommentRepository.Verify(repo => repo.AddCommentAsync(newComment), Times.Once);
            mockCommentRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }
        [Test]
        public async Task CreateCommentBatchAsync_AddsCommentsAndSavesChanges()
        {
            // Arrange
            var user = new User() { UserName = "test user name", Email = "testEmail@mail.com" };
            var newComments = new List<Comment>
            {
                new Comment { Text = "Comment 1", User = user },
                new Comment { Text = "Comment 2", User = user }
            };

            // Act
            await commentService.CreateCommentBatchAsync(newComments);

            // Assert
            mockUserService.Verify(repo => repo.CreateOrUpdateUserBatchAsync(It.IsAny<List<User>>()), Times.Once);
            mockCommentRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }
        [Test]
        public void CreateCommentAsync_ThrowsArgumentNullException_WhenCommentIsNull()
        {
            // Arrange
            Comment nullComment = null;

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await commentService.CreateCommentAsync(nullComment));
        }
    }
}

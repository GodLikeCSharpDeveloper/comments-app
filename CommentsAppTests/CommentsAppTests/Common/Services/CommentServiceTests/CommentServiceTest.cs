using CommentApp.Common.AutoMapper;
using CommentApp.Common.Models;
using CommentApp.Common.Models.DTOs;
using CommentApp.Common.Redis;
using CommentApp.Common.Repositories.CommentRepository;
using CommentApp.Common.Services.CommentService;
using CommentApp.Common.Services.UserService;
using Moq;

namespace CommentsAppTests.Common.Services.CommentServiceTests
{
    [TestFixture]
    public class CommentServiceTest
    {
        private Mock<ICommentRepository> mockCommentRepository;
        private Mock<IUserService> mockUserService;
        private Mock<IRedisUserCacheService> mockCacheService;
        private CommentService commentService;
        private Mock<IAutoMapperService> mockAutoMapperService;

        [SetUp]
        public void Setup()
        {
            mockCommentRepository = new Mock<ICommentRepository>();
            mockUserService = new Mock<IUserService>();
            mockCacheService = new Mock<IRedisUserCacheService>();
            mockAutoMapperService = new Mock<IAutoMapperService>();
            commentService = new CommentService(mockCommentRepository.Object, mockUserService.Object, mockCacheService.Object, mockAutoMapperService.Object);
        }

        [Test]
        public async Task GetCommentByIdAsync_ReturnsComment_WhenCommentExists()
        {
            // Arrange
            var expectedComment = new Comment { Text = "Test comment" };
            var resultComment = new GetCommentDto { Text = "Test comment" };
            mockCommentRepository
                .Setup(repo => repo.GetCommentByIdAsync(1))
                .ReturnsAsync(expectedComment);
            mockAutoMapperService.Setup(d => d.Map<Comment, GetCommentDto>(expectedComment)).Returns(resultComment);
            // Act
            var result = await commentService.GetCommentByIdAsync(1);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(resultComment));
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
        public async Task GetAllCommentsAsync_ReturnsAllComments()
        {
            // Arrange
            var comments = new List<Comment>
            {
                new Comment { Text = "Comment1" },
                new Comment { Text = "Comment2" }
            };
            var getComments = new List<GetCommentDto>
            {
                new GetCommentDto { Text = "Comment1" },
                new GetCommentDto { Text = "Comment2" }
            };
            mockCommentRepository.Setup(repo => repo.GetAllCommentsAsync())
                .ReturnsAsync(comments);
            mockAutoMapperService.Setup(d => d.Map<Comment, GetCommentDto>(comments)).Returns(getComments);
            // Act
            var result = await commentService.GetAllCommentsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result, Is.EqualTo(getComments));
            mockCommentRepository.Verify(repo => repo.GetAllCommentsAsync(), Times.Once);
        }

        [Test]
        public async Task GetLastAddedCommentForUser_ReturnsLastCommentId()
        {
            // Arrange
            string email = "user@mail.com";
            int lastCommentId = 5;
            mockCommentRepository.Setup(repo => repo.GetLastAddedCommentForUser(email))
                .ReturnsAsync(lastCommentId);

            // Act
            var result = await commentService.GetLastAddedCommentForUser(email);

            // Assert
            Assert.That(result, Is.EqualTo(lastCommentId));
            mockCommentRepository.Verify(repo => repo.GetLastAddedCommentForUser(email), Times.Once);
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
        public async Task CreateCommentAsync_AddsCommentWithCachedUser()
        {
            // Arrange
            var cachedUser = new User { Id = 1, Email = "cachedUser@mail.com", UserName = "CachedUser" };
            var newComment = new Comment { Text = "New Comment", User = cachedUser };

            mockCacheService.Setup(cache => cache.GetUserFromCache(cachedUser.Email))
                .ReturnsAsync(cachedUser);

            // Act
            await commentService.CreateCommentAsync(newComment);

            // Assert
            Assert.That(newComment.User, Is.Null);
            Assert.That(newComment.UserId, Is.EqualTo(cachedUser.Id));
            mockCommentRepository.Verify(repo => repo.AddCommentAsync(newComment), Times.Once);
            mockCommentRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
            mockCacheService.Verify(cache => cache.GetUserFromCache(cachedUser.Email), Times.Once);
            mockCacheService.Verify(cache => cache.AddUserToCache(It.IsAny<User>()), Times.Never);
        }

        [Test]
        public async Task CreateCommentAsync_AddsCommentAndCachesUser_WhenUserNotInCache()
        {
            // Arrange
            var newUser = new User { Id = 2, Email = "newUser@mail.com", UserName = "NewUser" };
            var newComment = new Comment { Id = 3, Text = "New Comment", User = newUser };

            mockCacheService.Setup(cache => cache.GetUserFromCache(newUser.Email))
                .ReturnsAsync((User)null);

            // Act
            await commentService.CreateCommentAsync(newComment);

            // Assert
            mockCommentRepository.Verify(repo => repo.AddCommentAsync(newComment), Times.Once);
            mockCommentRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
            mockCacheService.Verify(cache => cache.GetUserFromCache(newUser.Email), Times.Once);
            mockCacheService.Verify(cache => cache.AddUserToCache(newUser), Times.Once);
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

            // Mock ConvertCommentsToUsers method indirectly via userService
            var convertedUsers = new List<User> { user };
            mockUserService.Setup(service => service.CreateOrUpdateUserBatchAsync(It.IsAny<List<User>>()))
                .Returns(Task.CompletedTask)
                .Callback<List<User>>(u =>
                {
                    foreach (var usr in u)
                    {
                        usr.Id = 1; // Simulate assigning an ID after creation
                    }
                });

            // Act
            await commentService.CreateCommentBatchAsync(newComments);

            // Assert
            mockUserService.Verify(service => service.CreateOrUpdateUserBatchAsync(It.IsAny<List<User>>()), Times.Once);
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

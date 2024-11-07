using CommentApp.Common.Models;
using CommentApp.Common.Redis;
using CommentApp.Common.Repositories.CommentRepository;
using CommentApp.Common.Repositories.UserRepository;
using CommentApp.Common.Services.UserService;
using Moq;

namespace CommentsAppTests.Common.Services.UserServiceTests
{
    [TestFixture]
    public class UserServiceTests
    {
        private Mock<IUserRepository> mockUserRepository;
        private IUserService userService;
        private Mock<IRedisUserCacheService> mockRedisCacheService;
        private Mock<ICommentRepository> mockCommentRepository;

        [SetUp]
        public void Setup()
        {
            mockUserRepository = new Mock<IUserRepository>();
            mockRedisCacheService = new Mock<IRedisUserCacheService>();
            mockCommentRepository = new Mock<ICommentRepository>();
            userService = new UserService(mockUserRepository.Object, mockRedisCacheService.Object, mockCommentRepository.Object);
        }
        [Test]
        public async Task CreateCommentBatchAsync_SeparateUsersByExistingAndNonExisting()
        {
            // Arrange
            var comments = new List<Comment>
            {
                new Comment { Text = "Comment 1" },
                new Comment { Text = "Comment 2" },
                new Comment { Text = "Comment 3"},
                new Comment { Text = "Comment 4" },
                new Comment { Text = "Comment 5" }
            };
            var existingUser = new User() { UserName = "Test User1", Email = "testEmail1@mail.com" };
            var users = new List<User>()
            {
                new User() { UserName = "Test User2", Email = "testEmail2@mail.com", Comments = [comments[0], comments[1]] },
                new User() { UserName = "Test User3", Email = "testEmail3@mail.com", Comments = [comments[3]] },
                new User() { UserName = "Test User4", Email = "testEmail4@mail.com", Comments = [comments[4]] }
            };
            mockRedisCacheService.Setup(d => d.GetUserFromCache(It.IsAny<string>())).ReturnsAsync(existingUser);

            // Act
            users.Add(existingUser);
            await userService.CreateOrUpdateUserBatchAsync(users);

            // Assert
            mockCommentRepository.Verify(t => t.CreateCommentBatchAsync(It.IsAny<List<Comment>>()), Times.Once);
            mockRedisCacheService.Verify(t => t.AddUserToCache(It.IsAny<User>()), Times.Never);
            mockUserRepository.Verify(t => t.SaveChangesAsync(), Times.Once);
        }
    }
}

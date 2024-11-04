using CommentApp.Common.Data;
using CommentApp.Common.Models;
using CommentApp.Common.Repositories.CommentRepository;
using CommentApp.Common.Repositories.UserRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommentsAppTests.Common.Repositories.UserRepositoryTests
{
    [TestFixture]
    public class UserRepositoryTests
    {
        private CommentsAppDbContext dbContext;
        private UserRepository userRepository;

        [SetUp]
        public void Setup()
        {
            Batteries.Init();

            var options = new DbContextOptionsBuilder<CommentsAppDbContext>()
              .UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=TestDatabase;Integrated Security=True").Options;

            dbContext = new CommentsAppDbContext(options);
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            dbContext.Database.OpenConnection();
            userRepository = new UserRepository(dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            dbContext.Database.CloseConnection();
            dbContext.Dispose();
        }

        [Test]
        public async Task GetUserByIdAsync_ReturnsComment_WhenUserExists()
        {
            // Arrange
            var user = new User { UserName = "Test User", Email = "testEmail@mail.com" };


            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await userRepository.GetUserByIdAsync(user.Id);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Id, Is.EqualTo(user.Id));
                Assert.That(result.UserName, Is.EqualTo(user.UserName));
                Assert.That(result.Email, Is.EqualTo(user.Email));
            });
        }

        [Test]
        public async Task GetUserByIdAsync_ReturnsNull_WhenCommentDoesNotExist()
        {
            // Act
            var result = await userRepository.GetUserByIdAsync(999);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetUserByEmailAsync_ReturnsUser()
        {
            // Arrange
            var user = new User { UserName = "Test User", Email = "testEmail@mail.com" };


            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await userRepository.GetUserByEmail(user.Email);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Id, Is.EqualTo(user.Id));
                Assert.That(result.UserName, Is.EqualTo(user.UserName));
                Assert.That(result.Email, Is.EqualTo(user.Email));
            });
        }

        [Test]
        public async Task AddUserAsync_AddsUserToDatabase()
        {
            // Arrange
            var user = new User { UserName = "Test User", Email = "testEmail@mail.com" };

            // Act
            await userRepository.AddUserAsync(user);
            await userRepository.SaveChangesAsync();

            // Assert
            var result = await dbContext.Users.FindAsync(user.Id);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Email, Is.EqualTo(user.Email));
        }

        [Test]
        public async Task CreateCommentBatchAsync_AddsCommentsToDatabase()
        {
            // Arrange
            var comments = new List<Comment>
            {
                new Comment { Text = "Comment 1", Captcha = "captcha" },
                new Comment { Text = "Comment 2", Captcha = "captcha" },
                new Comment { Text = "Comment 3", Captcha = "captcha" },
                new Comment { Text = "Comment 4", Captcha = "captcha" },
                new Comment { Text = "Comment 5", Captcha = "captcha" }
            };
            var existingUser = new User() { UserName = "Test User1", Email = "testEmail1@mail.com" };
            var users = new List<User>()
            {
                new User() { UserName = "Test User2", Email = "testEmail2@mail.com", Comments = [comments[0], comments[1]] },
                new User() { UserName = "Test User3", Email = "testEmail3@mail.com", Comments = [comments[3]] },
                new User() { UserName = "Test User4", Email = "testEmail4@mail.com", Comments = [comments[4]] }
            };


            // Act
            var test = await userRepository.GetAllUsersAsync();
            await userRepository.AddUserAsync(existingUser);
            await dbContext.SaveChangesAsync();
            existingUser.Comments = [comments[2]];
            await userRepository.UpdateUserBatchAsync([existingUser]);
            await userRepository.CreateUserBatchAsync(users);
            await userRepository.SaveChangesAsync();

            // Assert
            var commentsFromDb = await dbContext.Comments.ToListAsync();
            var usersFromDb= await dbContext.Users.ToListAsync();
            Assert.Multiple(() =>
            {
                Assert.That(commentsFromDb, Has.Count.EqualTo(5));
                Assert.That(usersFromDb, Has.Count.EqualTo(4));
            });
        }
    }
}

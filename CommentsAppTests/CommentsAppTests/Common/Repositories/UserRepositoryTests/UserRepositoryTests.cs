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
        public async Task GetUserByIdAsync_ReturnsUser_WhenUserExists()
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
        public async Task GetUserByIdAsync_ReturnsNull_WhenUserDoesNotExist()
        {
            // Act
            var result = await userRepository.GetUserByIdAsync(999);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetUserByEmailAsync_ReturnsUser_WhenEmailExists()
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
        public async Task CreateUserBatchAsync_AddsUsersToDatabase()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserName = "User1", Email = "user1@mail.com" },
                new User { UserName = "User2", Email = "user2@mail.com" },
                new User { UserName = "User3", Email = "user3@mail.com" }
            };

            // Act
            await userRepository.CreateUserBatchAsync(users);
            await userRepository.SaveChangesAsync();

            // Assert
            var usersFromDb = await dbContext.Users.ToListAsync();
            Assert.That(usersFromDb, Has.Count.EqualTo(3));
            foreach (var user in users)
            {
                Assert.That(usersFromDb.Any(u => u.Email == user.Email), Is.True);
            }
        }

        [Test]
        public async Task UpdateUserBatchAsync_UpdatesUsersInDatabase()
        {
            // Arrange
            var users = new List<User>
            {
                new User { UserName = "User1", Email = "user7@mail.com" },
                new User { UserName = "User2", Email = "user8@mail.com" }
            };

            await dbContext.Users.AddRangeAsync(users);
            await dbContext.SaveChangesAsync();

            // Modify users
            users[0].UserName = "UpdatedUser1";
            users[1].Email = "updatedUser2@mail.com";

            // Act
            await userRepository.UpdateUserBatchAsync(users);
            await userRepository.SaveChangesAsync();

            // Assert
            var updatedUser1 = await dbContext.Users.FindAsync(users[0].Id);
            var updatedUser2 = await dbContext.Users.FindAsync(users[1].Id);

            Assert.Multiple(() =>
            {
                Assert.That(updatedUser1.UserName, Is.EqualTo("UpdatedUser1"));
                Assert.That(updatedUser2.Email, Is.EqualTo("updatedUser2@mail.com"));
            });
        }

        [Test]
        public async Task GetUsersAsync_ReturnsAllUsers_WithComments()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                {
                    UserName = "User1",
                    Email = "user1@mail.com",
                    Comments = new List<Comment>
                    {
                        new Comment { Text = "Comment1" },
                        new Comment { Text = "Comment2" }
                    }
                },
                new User
                {
                    UserName = "User2",
                    Email = "user2@mail.com",
                    Comments = new List<Comment>
                    {
                        new Comment { Text = "Comment3" }
                    }
                }
            };

            await dbContext.Users.AddRangeAsync(users);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await userRepository.GetUsersAsync();

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
            foreach (var user in result)
            {
                if (user.UserName == "User1")
                {
                    Assert.That(user.Comments, Has.Count.EqualTo(2));
                }
                else if (user.UserName == "User2")
                {
                    Assert.That(user.Comments, Has.Count.EqualTo(1));
                }
            }
        }

        [Test]
        public async Task GetAllUsersAsync_ReturnsAllUsers_WithComments()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                {
                    UserName = "User1",
                    Email = "user1@mail.com",
                    Comments = new List<Comment>
                    {
                        new Comment { Text = "Comment1" }
                    }
                },
                new User
                {
                    UserName = "User2",
                    Email = "user2@mail.com",
                    Comments = new List<Comment>
                    {
                        new Comment { Text = "Comment2" },
                        new Comment { Text = "Comment3" }
                    }
                }
            };

            await dbContext.Users.AddRangeAsync(users);
            await dbContext.SaveChangesAsync();

            // Act
            var result = await userRepository.GetAllUsersAsync();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(2));
            foreach (var user in result)
            {
                if (user.UserName == "User1")
                {
                    Assert.That(user.Comments, Has.Count.EqualTo(1));
                }
                else if (user.UserName == "User2")
                {
                    Assert.That(user.Comments, Has.Count.EqualTo(2));
                }
            }
        }

        [Test]
        public async Task UpdateUserBatchAsync_DoesNotThrow_WhenUpdatingNonExistentUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Id = 999, UserName = "NonExistentUser", Email = "nonexistent@mail.com" }
            };

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
            {
                await userRepository.UpdateUserBatchAsync(users);
                await userRepository.SaveChangesAsync();
            });
        }

        [Test]
        public async Task AddUserAsync_ThrowsException_WhenAddingDuplicateEmail()
        {
            // Arrange
            var user1 = new User { UserName = "User1", Email = "duplicate@mail.com" };
            var user2 = new User { UserName = "User2", Email = "duplicate@mail.com" };

            await dbContext.Users.AddAsync(user1);
            await dbContext.SaveChangesAsync();

            // Act & Assert
            await userRepository.AddUserAsync(user2);
            Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await userRepository.SaveChangesAsync();
            });
        }
    }
}

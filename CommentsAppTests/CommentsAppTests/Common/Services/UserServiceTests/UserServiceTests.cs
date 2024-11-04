﻿using CommentApp.Common.Models;
using CommentApp.Common.Redis;
using CommentApp.Common.Repositories.CommentRepository;
using CommentApp.Common.Repositories.UserRepository;
using CommentApp.Common.Services.CommentService;
using CommentApp.Common.Services.UserService;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommentsAppTests.Common.Services.UserServiceTests
{
    [TestFixture]
    public class UserServiceTests
    {
        private Mock<IUserRepository> mockUserRepository;
        private IUserService userService;
        private Mock<IRedisUserCacheService> mockRedisCacheService;

        [SetUp]
        public void Setup()
        {
            mockUserRepository = new Mock<IUserRepository>();
            mockRedisCacheService = new Mock<IRedisUserCacheService>();
            userService = new UserService(mockUserRepository.Object, mockRedisCacheService.Object);
        }
        [Test]
        public async Task CreateCommentBatchAsync_SeparateUsersByExistingAndNonExisting()
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
            mockRedisCacheService.Setup(d => d.GetUserFromCache(It.IsAny<string>())).ReturnsAsync(existingUser);

            // Act
            users.Add(existingUser);
            await userService.CreateOrUpdateUserBatchAsync(users);

            // Assert
            mockUserRepository.Verify(t=>t.CreateUserBatchAsync(It.IsAny<List<User>>()), Times.Once);
            mockUserRepository.Verify(t => t.UpdateUserBatchAsync(It.IsAny<List<User>>()), Times.Once);
        }
    }
}
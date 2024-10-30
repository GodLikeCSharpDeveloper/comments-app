using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Confluent.Kafka;
using CommentApp.Common.Controllers;
using CommentApp.Common.Kafka.Producer;
using CommentApp.Common.Models.DTOs;
using CommentApp.Common.Models;
using Microsoft.AspNetCore.Http;
using CommentApp.Common.Services.FileService;
using CommentApp.Common.AutoMapper;
namespace CommentsAppTests
{
    [TestFixture]
    public class CommentsControllerTests
    {
        private Mock<ILogger<CommentsController>> _loggerMock;
        private Mock<IAutoMapperService> _mapperMock;
        private Mock<IKafkaQueueService> _kafkaMock;
        private CommentsController _controller;
        private Mock<IFileService> _fileServiceMock;
        private Mock<IBackgroundTaskQueue> _backgroundTaskQueueMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<CommentsController>>();
            _mapperMock = new Mock<IAutoMapperService>();
            _kafkaMock = new Mock<IKafkaQueueService>();
            _fileServiceMock = new Mock<IFileService>();
            _backgroundTaskQueueMock = new Mock<IBackgroundTaskQueue>();
            _controller = new CommentsController(_loggerMock.Object, _mapperMock.Object, _fileServiceMock.Object, _kafkaMock.Object, _backgroundTaskQueueMock.Object);
        }

        private void AddModelErrors(Dictionary<string, string> errors)
        {
            foreach (var error in errors)
            {
                _controller.ModelState.AddModelError(error.Key, error.Value);
            }
        }

        private void AssertBadRequest(Dictionary<string, string> expectedErrors, IActionResult result)
        {
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult?.StatusCode, Is.EqualTo(400));

            var modelState = _controller.ModelState;
            Assert.Multiple(() =>
            {
                foreach (var error in expectedErrors)
                {
                    Assert.That(modelState.ContainsKey(error.Key), $"ModelState should contain error for '{error.Key}'");
                    Assert.That(modelState[error.Key].Errors.Count, Is.EqualTo(1));
                    Assert.That(modelState[error.Key].Errors[0].ErrorMessage, Is.EqualTo(error.Value));
                }
            });
        }

        [Test]
        public async Task PostComment_InvalidModel_MissingRequiredFields_ReturnsBadRequest()
        {
            // Arrange
            var invalidRequest = new CreateCommentDto
            {
                Text = null,
                Captcha = null,
                UserName = null,
                Email = null
            };

            var errors = new Dictionary<string, string>
        {
            { "Text", "Text is required" },
            { "Captcha", "Captcha is required" },
            { "UserName", "UserName is required" },
            { "Email", "Email is required" }
        };
            AddModelErrors(errors);

            // Act
            var result = await _controller.PostComment(invalidRequest);

            // Assert
            AssertBadRequest(errors, result);
        }

        [Test]
        public async Task PostComment_InvalidModel_FieldLengthExceeded_ReturnsBadRequest()
        {
            // Arrange
            var invalidRequest = new CreateCommentDto
            {
                Text = new string('b', 201),
                Captcha = new string('c', 21),
                UserName = new string('u', 51),
                Email = new string('e', 51)
            };

            var errors = new Dictionary<string, string>
        {
            { "Text", "Text cannot exceed 200 characters" },
            { "Captcha", "Captcha cannot exceed 20 characters" },
            { "UserName", "UserName cannot exceed 50 characters" },
            { "Email", "Email cannot exceed 50 characters" }
        };
            AddModelErrors(errors);

            // Act
            var result = await _controller.PostComment(invalidRequest);

            // Assert
            AssertBadRequest(errors, result);
        }

        [Test]
        public async Task PostComment_ValidModel_ReturnsOk()
        {
            // Arrange
            IFormFile imageFile = new Mock<IFormFile>().Object;
            var validRequest = new CreateCommentDto
            {
                Text = "Valid text",
                Captcha = "ValidCaptcha",
                UserName = "ValidUser",
                Email = "user@example.com",
                Image = imageFile

            };
            var comment = new Comment
            {
                Text = validRequest.Text,
                Captcha = validRequest.Captcha,
                User = new User
                {
                    UserName = validRequest.UserName,
                    Email = validRequest.Email
                },
                ImageUrl = "TestFileName"
            };
            _mapperMock.Setup(m => m.Map<CreateCommentDto, Comment>(validRequest)).Returns(comment);
            _kafkaMock.Setup(k => k.EnqueueMessageAsync(It.IsAny<Message<Null, string>>(), It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.PostComment(validRequest);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<OkResult>());
                Assert.That(string.IsNullOrEmpty(comment.ImageUrl), Is.Not.True);
            });
            _kafkaMock.Verify(k => k.EnqueueMessageAsync(It.IsAny<Message<Null, string>>(), It.IsAny<CancellationToken>()), Times.Once);
            _backgroundTaskQueueMock.Verify(k => k.QueueBackgroundWorkItem(It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
        }
    }
}
using CommentApp.Common.AutoMapper;
using CommentApp.Common.Controllers;
using CommentApp.Common.Kafka.Producer;
using CommentApp.Common.Models;
using CommentApp.Common.Models.DTOs;
using CommentApp.Common.Services.CaptchaService;
using CommentApp.Common.Services.CommentService;
using CommentApp.Common.Services.FileService;
using CommentApp.Common.Services.FileService.FileProcessingService;
using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
namespace CommentsAppTests.Common.Controllers
{
    [TestFixture]
    public class CommentsControllerTests
    {
        private Mock<ILogger<CommentsController>> _loggerMock;
        private Mock<IAutoMapperService> _mapperMock;
        private Mock<IKafkaQueueService> _kafkaMock;
        private CommentsController _controller;
        private Mock<IFileService> _fileServiceMock;
        private Mock<ICommentService> _commentServiceMock;
        private Mock<ICaptchaService> _captchaServiceMock;
        private Mock<IFileProcessingService> _fileProcessingServiceMock;
        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<CommentsController>>();
            _mapperMock = new Mock<IAutoMapperService>();
            _kafkaMock = new Mock<IKafkaQueueService>();
            _fileServiceMock = new Mock<IFileService>();
            _commentServiceMock = new Mock<ICommentService>();
            _captchaServiceMock = new Mock<ICaptchaService>();
            _fileProcessingServiceMock = new Mock<IFileProcessingService>();
            _controller = new CommentsController(
                _loggerMock.Object,
                _mapperMock.Object,
                _fileServiceMock.Object,
                _kafkaMock.Object,
                _commentServiceMock.Object,
                _captchaServiceMock.Object,
                _fileProcessingServiceMock.Object);
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
                    Assert.That(modelState[error.Key].Errors, Has.Count.EqualTo(1));
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
                UserName = "ValidUser",
                Email = "user@example.com",
                Image = imageFile

            };
            var comment = new Comment
            {
                Text = validRequest.Text,
                User = new User
                {
                    UserName = validRequest.UserName,
                    Email = validRequest.Email
                },
                ImageUrl = "TestFileName"
            };
            _mapperMock.Setup(m => m.Map<CreateCommentDto, Comment>(validRequest)).Returns(comment);
            _fileProcessingServiceMock.Setup(k => k.GetNewNameAndUploadFile(It.IsAny<IFormFile>()))
                      .Returns("TestFileName");

            // Act
            var result = await _controller.PostComment(validRequest);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<OkResult>());
                Assert.That(string.IsNullOrEmpty(comment.ImageUrl), Is.Not.True);
            });
            _kafkaMock.Verify(k => k.EnqueueMessageAsync(It.IsAny<Message<Null, string>>(), It.IsAny<CancellationToken>()), Times.Once);
            _fileProcessingServiceMock.Verify(k => k.GetNewNameAndUploadFile(It.IsAny<IFormFile>()), Times.Once);
        }
        [Test]
        public async Task GetLastAddedComment_PassingNullOrEmtpy_ReturnsBadRequest()
        {
            // Arrange & Act
            var result = await _controller.GetLastAddedCommentForUser(string.Empty);

            //Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Email is required"));
        }
        [Test]
        public async Task GetLastAddedCommentForUser_WithValidEmail_ReturnsOkWithCommentId()
        {
            // Arrange
            string email = "test@example.com";
            var expectedCommentId = 123;
            _commentServiceMock.Setup(s => s.GetLastAddedCommentForUser(email))
                .ReturnsAsync(expectedCommentId);

            // Act
            var result = await _controller.GetLastAddedCommentForUser(email);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult.Value, Is.EqualTo(expectedCommentId));
        }
        [Test]
        public async Task GetComments_WithValidQueryParameters_ReturnsOkWithComments()
        {
            // Arrange
            var queryParameters = new CommentQueryParameters();
            var expectedResponse = new List<Comment> { new Comment { Text = "Test Comment" } };
            _commentServiceMock.Setup(s => s.GetCommentsByQueryAsync(queryParameters))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetComments(queryParameters);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.AreEqual(expectedResponse, okResult.Value);
        }

        [Test]
        public async Task GetAllComments_ReturnsOkWithAllComments()
        {
            // Arrange
            var expectedResponse = new List<Comment> { new() { Text = "Test Comment" } };
            _commentServiceMock.Setup(s => s.GetAllCommentsAsync())
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAllComments();

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult.Value, Is.EqualTo(expectedResponse));
        }

        [Test]
        public async Task CountComments_ReturnsOkWithCommentCount()
        {
            // Arrange
            int expectedCount = 10;
            _commentServiceMock.Setup(s => s.CountAllComments())
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _controller.CountComments();

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult.Value, Is.EqualTo(expectedCount));
        }

        [Test]
        public async Task GetPresignedUrl_WithEmptyFilePath_ReturnsBadRequest()
        {
            // Arrange
            string filePath = string.Empty;

            // Act
            var result = await _controller.GetPresignedUrl(filePath);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult.Value, Is.EqualTo("File path is required."));
        }

        [Test]
        public async Task GetPresignedUrl_WithValidFilePath_ReturnsOkWithPresignedUrl()
        {
            // Arrange
            string filePath = "test/path";
            string expectedUrl = "https://example.com/presigned-url";
            _fileServiceMock.Setup(s => s.GeneratePreSignedURL(filePath))
                .Returns(expectedUrl);

            // Act
            var result = await _controller.GetPresignedUrl(filePath);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult.Value, Is.EqualTo(expectedUrl));
        }

        [Test]
        public async Task ValidateCaptcha_WithValidToken_ReturnsOkWithValidationResult()
        {
            // Arrange
            string token = "valid-token";
            bool expectedValidationResult = true;
            _captchaServiceMock.Setup(s => s.ValidateToken(token))
                .ReturnsAsync(expectedValidationResult);

            // Act
            var result = await _controller.ValidateCaptcha(token);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult.Value, Is.EqualTo(expectedValidationResult));
        }
    }
}
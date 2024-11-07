using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;

namespace CommentsAppTests.Common.Services.FileServiceTests
{
    [TestFixture]
    public class AmazonFileServiceTests
    {
        private AmazonS3FileService _fileService;
        private Mock<AmazonS3Client> _mockAmazonS3Client;
        private Mock<ILogger<AmazonS3FileService>> _loggerMock;
        private Mock<IFormFile> mockFormFile;
        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<AmazonS3FileService>>();
            _mockAmazonS3Client = new Mock<AmazonS3Client>();
            _fileService = new(_mockAmazonS3Client.Object, _loggerMock.Object);
            mockFormFile = new Mock<IFormFile>();
        }
        [Test]
        public async Task UploadFile_NullValue_ShoudReturnImmediatly()
        {
            // Act
            await _fileService.UploadFileAsync(null, It.IsAny<string>(), It.IsAny<string>());

            // Assert
            _mockAmazonS3Client.Verify(p => p.PutObjectAsync(It.IsAny<PutObjectRequest>(), new CancellationToken()), Times.Never);
        }
        [Test]
        public void UploadFile_ThrowsInternalServerError_ShouldRetryThreeTimes()
        {
            // Arrange;
            var data = "Test data";
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            _mockAmazonS3Client
                .Setup(p => p.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutObjectResponse
                {
                    HttpStatusCode = HttpStatusCode.InternalServerError
                });

            // Act
            var exception = Assert.ThrowsAsync<AmazonS3Exception>(async () =>
                await _fileService.UploadFileAsync(memoryStream, "test-file-name", It.IsAny<string>()));

            //Assert
            Assert.That(exception, Is.InstanceOf<AmazonS3Exception>());
            Assert.That(exception.Message, Is.EqualTo("Error while uploading file to S3."));

            _mockAmazonS3Client.Verify(
                p => p.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(4));
        }

        [Test]
        public void UploadFile_ReturnsNotHttpOk_ThrowsException()
        {
            //Arrange
            var data = "Test data";
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            _mockAmazonS3Client
                .Setup(p => p.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutObjectResponse
                {
                    HttpStatusCode = HttpStatusCode.InternalServerError
                });

            // Act & Assert
            Assert.ThrowsAsync<AmazonS3Exception>(async () => await _fileService.UploadFileAsync(memoryStream, It.IsAny<string>(), It.IsAny<string>()));
        }
    }
}

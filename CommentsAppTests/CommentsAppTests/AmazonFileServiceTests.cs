using Amazon.S3;
using Amazon.S3.Model;
using CommentApp.Common.AutoMapper;
using CommentApp.Common.Controllers;
using CommentApp.Common.Kafka.Producer;
using CommentApp.Common.Models.DTOs;
using CommentApp.Common.Services.FileService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommentsAppTests
{
    [TestFixture]
    public class AmazonFileServiceTests
    {
        private AmazonS3FileService _fileService;
        private Mock<AmazonS3Client> _mockAmazonS3Client;
        private Mock<ILogger<CommentsController>> _loggerMock;
        private Mock<IFormFile> mockFormFile;
        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<CommentsController>>();
            _mockAmazonS3Client = new Mock<AmazonS3Client>();
            _fileService = new(_mockAmazonS3Client.Object, _loggerMock.Object, It.IsAny<string>());
            mockFormFile = new Mock<IFormFile>();
        }
        [Test]
        public async Task UploadFile_NullValue_ShoudReturnImmediatly()
        {
            // Act
            await _fileService.UploadFileAsync(null, It.IsAny<string>());

            // Assert
            _mockAmazonS3Client.Verify(p => p.PutObjectAsync(It.IsAny<PutObjectRequest>(), new CancellationToken()), Times.Never);
        }
        [Test]
        public async Task UploadFile_ThrowsInternalServerError_ShouldRetryThreeTimes()
        {
            // Arrange
            var mockFormFile = new Mock<IFormFile>();
            mockFormFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
            mockFormFile.Setup(f => f.ContentType).Returns("application/octet-stream");

            _mockAmazonS3Client
                .Setup(p => p.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutObjectResponse
                {
                    HttpStatusCode = HttpStatusCode.InternalServerError
                });

            // Act
            var exception = Assert.ThrowsAsync<AmazonS3Exception>(async () =>
                await _fileService.UploadFileAsync(mockFormFile.Object, "test-file-name"));

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
            _mockAmazonS3Client
                .Setup(p => p.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PutObjectResponse
                {
                    HttpStatusCode = HttpStatusCode.InternalServerError
                });

            // Act & Assert
            Assert.ThrowsAsync<AmazonS3Exception>(async () => await _fileService.UploadFileAsync(mockFormFile.Object, It.IsAny<string>()));
        }
    }
}

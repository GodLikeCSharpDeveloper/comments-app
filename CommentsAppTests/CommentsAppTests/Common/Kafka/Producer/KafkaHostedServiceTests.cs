using CommentApp.Common.Kafka.Producer;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Channels;
namespace CommentsAppTests.Common.Kafka.Producer
{
    [TestFixture]
    public class KafkaHostedServiceTests
    {
        private Mock<IProducer<Null, string>> mockProducer;
        private Mock<ILogger<KafkaHostedService>> mockLogger;
        private Mock<IKafkaQueueService> mockKafkaQueueService;
        private Channel<Message<Null, string>> messageChannel;

        [SetUp]
        public void Setup()
        {
            mockProducer = new Mock<IProducer<Null, string>>();
            mockLogger = new Mock<ILogger<KafkaHostedService>>();
            mockKafkaQueueService = new Mock<IKafkaQueueService>();

            messageChannel = Channel.CreateUnbounded<Message<Null, string>>();
            mockKafkaQueueService.Setup(k => k.MessageChannel).Returns(messageChannel);
        }

        [Test]
        public async Task ExecuteAsync_SendsMessageSuccessfully()
        {
            // Arrange
            var message = new Message<Null, string> { Value = "Test message" };
            var deliveryResult = new DeliveryResult<Null, string>
            {
                Topic = "comments-new",
                Partition = new Partition(0),
                Offset = new Offset(0),
                Message = message
            };

            mockProducer
                .Setup(p => p.ProduceAsync("comments-new", message, It.IsAny<CancellationToken>()))
                .ReturnsAsync(deliveryResult);

            var service = new KafkaHostedService(mockProducer.Object, mockLogger.Object, mockKafkaQueueService.Object);

            // Act
            var cts = new CancellationTokenSource();
            var executeTask = service.StartAsync(cts.Token);

            await messageChannel.Writer.WriteAsync(message);

            await Task.Delay(100);

            cts.Cancel();
            await service.StopAsync(CancellationToken.None);

            // Assert
            mockProducer.Verify(
                p => p.ProduceAsync("comments-new", message, It.IsAny<CancellationToken>()),
                Times.Once);

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Message sent to Kafka")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_HandlesProduceException()
        {
            // Arrange
            var message = new Message<Null, string> { Value = "Test message" };
            var produceException = new ProduceException<Null, string>(new Error(ErrorCode.Local_MsgTimedOut, "Timed out"), null);

            mockProducer
                .Setup(p => p.ProduceAsync("comments-new", message, It.IsAny<CancellationToken>()))
                .ThrowsAsync(produceException);

            var service = new KafkaHostedService(mockProducer.Object, mockLogger.Object, mockKafkaQueueService.Object);

            // Act
            var cts = new CancellationTokenSource();
            var executeTask = service.StartAsync(cts.Token);

            await messageChannel.Writer.WriteAsync(message);

            await Task.Delay(100);

            cts.Cancel();
            await service.StopAsync(CancellationToken.None);

            // Assert
            mockProducer.Verify(
                p => p.ProduceAsync("comments-new", message, It.IsAny<CancellationToken>()),
                Times.Once);

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Kafka produce error")),
                    produceException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_HandlesGeneralException()
        {
            // Arrange
            var message = new Message<Null, string> { Value = "Test message" };
            var generalException = new Exception("General error");

            mockProducer
                .Setup(p => p.ProduceAsync("comments-new", message, It.IsAny<CancellationToken>()))
                .ThrowsAsync(generalException);

            var service = new KafkaHostedService(mockProducer.Object, mockLogger.Object, mockKafkaQueueService.Object);

            // Act
            var cts = new CancellationTokenSource();
            var executeTask = service.StartAsync(cts.Token);

            await messageChannel.Writer.WriteAsync(message);

            await Task.Delay(100);

            cts.Cancel();
            await service.StopAsync(CancellationToken.None);

            // Assert
            mockProducer.Verify(
                p => p.ProduceAsync("comments-new", message, It.IsAny<CancellationToken>()),
                Times.Once);

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Unexpected error while sending message to Kafka")),
                    generalException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_RespectsCancellationToken()
        {
            // Arrange
            var service = new KafkaHostedService(mockProducer.Object, mockLogger.Object, mockKafkaQueueService.Object);

            // Act
            var cts = new CancellationTokenSource();
            var executeTask = service.StartAsync(cts.Token);

            cts.Cancel();

            await executeTask;

            // Assert
            Assert.That(executeTask.IsCompleted, Is.True);
        }

        [Test]
        public async Task StopAsync_CompletesMessageChannel()
        {
            // Arrange
            var service = new KafkaHostedService(mockProducer.Object, mockLogger.Object, mockKafkaQueueService.Object);

            // Act
            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);

            // Assert
            Assert.That(messageChannel.Reader.Completion.IsCompleted, Is.True);
        }
    }
}
﻿using CommentApp.Common.Kafka.Consumer;
using CommentApp.Common.Kafka.TopicCreator;
using CommentApp.Common.Models.Options;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;

namespace CommentsAppTests.Common.Kafka.Consumer
{
    [TestFixture]
    public class CommentConsumerTests
    {
        private TestableCommentConsumerService _commentConsumer;
        private Mock<IConsumer<Null, string>> _mockConsumer;
        private Mock<ILogger<CommentConsumer>> _loggerMock;
        private Mock<IKafkaTopicCreator> _mockKafkaTopicCreator;
        private Mock<IDatabase> _mockRedisDatabase;
        private Mock<IServiceScopeFactory> _mockServiceScopeFactory;
        private Mock<IServiceScope> _mockServiceScope;
        [SetUp]
        public void Setup()
        {
            _mockConsumer = new Mock<IConsumer<Null, string>>();
            _mockRedisDatabase = new Mock<IDatabase>();
            _mockKafkaTopicCreator = new Mock<IKafkaTopicCreator>();
            _loggerMock = new Mock<ILogger<CommentConsumer>>();
            _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            _mockServiceScope = new Mock<IServiceScope>();
            var _mockServiceProvider = new Mock<IServiceProvider>();
            _mockConsumer.Setup(p => p.Consume(new CancellationToken())).Returns(new ConsumeResult<Null, string>() { Message = new Message<Null, string>() { Value = "Test value" } });
            _mockServiceScopeFactory.Setup(r => r.CreateScope()).Returns(_mockServiceScope.Object);

            var consumerOptions = new ConsumerOptions()
            {
                CommentConsumerQueueKey = "test key",
                MaxRetryCount = 3,
                MaxRetryDelayMilliseconds = 500,
                RetryDelayMilliseconds = 200
            };
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IOptions<ConsumerOptions>)))
                .Returns(Options.Create(consumerOptions));
            _mockServiceScope.Setup(r => r.ServiceProvider).Returns(_mockServiceProvider.Object);
            _commentConsumer = new TestableCommentConsumerService(
                _mockConsumer.Object, _loggerMock.Object, _mockKafkaTopicCreator.Object, _mockRedisDatabase.Object, _mockServiceScopeFactory.Object
            );
        }

        [Test]
        public async Task BackgroundRunning_CancellationRequested_ShouldStopImmediately()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            // Act
            await _commentConsumer.ExecuteAsync(cancellationToken.Token);

            // Assert
            _mockRedisDatabase.Verify(
                p => p.ListRightPushAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<When>(), CommandFlags.None),
                Times.Never
            );
            _loggerMock.Verify(l => l.Log(
                It.Is<LogLevel>(lvl => lvl == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Max retry attempts exceeded. Background service is stopping.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [Test]
        public async Task BackgroundRunning_ThrowsException_ShouldRetry()
        {
            // Arrange
            var maxTries = 4;

            _mockRedisDatabase.Setup(p => p.ListRightPushAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<When>(), CommandFlags.None))
                .ThrowsAsync(new RedisServerException("Redis server error"));

            // Act
            await _commentConsumer.ExecuteAsync(new CancellationToken());

            // Assert
            _mockRedisDatabase.Verify(p => p.ListRightPushAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<When>(), CommandFlags.None), Times.Exactly(maxTries));

        }
        public class TestableCommentConsumerService : CommentConsumer
        {
            public TestableCommentConsumerService(IConsumer<Null, string> consumer,
                ILogger<CommentConsumer> logger,
                IKafkaTopicCreator kafkaTopicCreator,
                IDatabase redisDatabase,
                IServiceScopeFactory serviceScopeFactory) : base(consumer, logger, kafkaTopicCreator, redisDatabase, serviceScopeFactory)
            {
            }

            public new Task ExecuteAsync(CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(cancellationToken);
            }
        }
    }
}

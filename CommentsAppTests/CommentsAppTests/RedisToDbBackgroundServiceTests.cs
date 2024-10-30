﻿using CommentApp.Common.Models;
using CommentApp.Common.Redis;
using CommentApp.Common.Services.CommentService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CommentsAppTests
{
    [TestFixture]
    public class RedisToDbBackgroundServiceTests
    {
        private TestableRedisService _redisService;
        private Mock<IDatabase> _mockRedisDatabase;
        private Mock<ILogger<RedisToDbBackgroundService>> _loggerMock;
        private Mock<IServiceScopeFactory> _mockScopeFactory;
        private Mock<IOptions<BackgroundRedisOptions>> _mockRedisOptions;
        private Mock<ICommentService> _mockCommentService;
        private Queue<RedisValue> _redisList;

        [SetUp]
        public void Setup()
        {
            // Инициализация моков
            _loggerMock = new Mock<ILogger<RedisToDbBackgroundService>>();
            _mockRedisDatabase = new Mock<IDatabase>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockRedisOptions = new Mock<IOptions<BackgroundRedisOptions>>();
            _mockCommentService = new Mock<ICommentService>();

            // Настройка BackgroundRedisOptions
            var backgroundRedisOptions = new BackgroundRedisOptions()
            {
                BatchSize = 10,
                RedisKeyValue = "testKey",
                MaxRetryAttempts = 3,
                RetryDelayMilliseconds = 100,
                SingleProcessingThreshold = 3
            };
            _mockRedisOptions.Setup(r => r.Value).Returns(backgroundRedisOptions);

            // Инициализация тестируемого сервиса
            _redisService = new TestableRedisService(
                _loggerMock.Object,
                _mockRedisDatabase.Object,
                _mockScopeFactory.Object,
                _mockRedisOptions.Object
            );

            // Инициализация очереди Redis
            _redisList = new Queue<RedisValue>();

            // Настройка методов IDatabase
            _mockRedisDatabase.Setup(r => r.ListLengthAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync((RedisKey key, CommandFlags flags) => _redisList.Count);

            _mockRedisDatabase.Setup(r => r.ListLeftPopAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync((RedisKey key, CommandFlags flags) =>
                {
                    if (_redisList.Count > 0)
                        return _redisList.Dequeue();
                    else
                        return RedisValue.Null;
                });

            _mockRedisDatabase.Setup(r => r.ListLeftPushAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync((RedisKey key, RedisValue value, When when, CommandFlags flags) =>
                {
                    _redisList.Enqueue(value);
                    return _redisList.Count;
                });

            // Настройка IServiceScopeFactory
            var mockServiceScope = new Mock<IServiceScope>();
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(sp => sp.GetService(typeof(ICommentService))).Returns(_mockCommentService.Object);
            mockServiceScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
            _mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockServiceScope.Object);
        }

        [Test]
        public async Task BackgroundRunning_CancellationRequested_ShouldStopImmediately()
        {
            // Arrange
            var cancellationToken = new CancellationTokenSource();
            cancellationToken.Cancel();

            // Act
            await _redisService.ExecuteAsync(cancellationToken.Token);

            // Assert
            _mockRedisDatabase.Verify(
                p => p.ListLeftPopAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()),
                Times.Never
            );

            // Проверка логирования остановки сервиса
            _loggerMock.Verify(l => l.Log(
                It.Is<LogLevel>(lvl => lvl == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Redis to DB Background Service is stopping.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once
            );
        }

        [Test]
        public async Task BackgroundRunning_ThrowsException_ShouldRetry()
        {
            // Arrange
            var maxTries = _mockRedisOptions.Object.Value.MaxRetryAttempts;
            _mockRedisDatabase.Setup(p => p.ListLengthAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(0);

            _mockRedisDatabase.Setup(p => p.ListLeftPopAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ThrowsAsync(new RedisServerException("Redis server error"));

            // Act
            await _redisService.ExecuteAsync(new CancellationToken());

            // Assert
            _mockRedisDatabase.Verify(p => p.ListLengthAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Exactly(maxTries + 1));

            // Проверка логирования критической ошибки после превышения попыток
            _loggerMock.Verify(l => l.Log(
                It.Is<LogLevel>(lvl => lvl == LogLevel.Critical),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Max retry attempts exceeded. Background service is stopping.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once
            );
        }

        [Test]
        public async Task ExecuteAsync_ProcessSingleComment_ShouldCreateCommentOnce()
        {
            // Arrange
            var tcs = new TaskCompletionSource<bool>();

            var comment = new Comment { Id = 1, Text = "Test comment" };
            var serializedComment = JsonSerializer.Serialize(comment);
            _redisList.Enqueue(serializedComment);

            // Настройка мокнутого ICommentService, чтобы сигнализировать о вызове CreateCommentAsync
            _mockCommentService.Setup(s => s.CreateCommentAsync(It.Is<Comment>(c => c.Id == 1 && c.Text == "Test comment")))
                .Callback(() => tcs.SetResult(true))
                .Returns(Task.CompletedTask);

            // Act
            var cancellationTokenSource = new CancellationTokenSource();
            var executeTask = _redisService.ExecuteAsync(cancellationTokenSource.Token);

            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));

            if (completedTask == tcs.Task)
            {
                cancellationTokenSource.Cancel();
            }
            else
            {
                Assert.Fail("CreateCommentAsync was not called within the expected time.");
            }
            await executeTask;

            // Assert
            _mockCommentService.Verify(s => s.CreateCommentAsync(It.Is<Comment>(c => c.Id == 1 && c.Text == "Test comment")), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_ProcessBatchComments_ShouldCreateBatchOnce()
        {
            // Arrange
            var tcs = new TaskCompletionSource<bool>();

            for (int i = 0; i < 5; i++)
            {
                var comment = new Comment { Id = i, Text = $"Test comment {i}" };
                var serializedComment = JsonSerializer.Serialize(comment);
                _redisList.Enqueue(serializedComment);
            }

            _mockCommentService.Setup(s => s.CreateCommentBatchAsync(It.Is<List<Comment>>(list => list.Count == 5 && list.All(c => c.Text.StartsWith("Test comment")))))
                .Callback(() => tcs.SetResult(true))
                .Returns(Task.CompletedTask);

            _mockRedisDatabase.Setup(p => p.ListLengthAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(5);

            _mockRedisDatabase.Setup(p => p.ListLeftPopAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync((RedisKey key, CommandFlags flags) => _redisList.Count > 0 ? _redisList.Dequeue() : RedisValue.Null);

            // Act
            var cancellationTokenSource = new CancellationTokenSource();
            var executeTask = _redisService.ExecuteAsync(cancellationTokenSource.Token);

            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(5000));

            if (completedTask == tcs.Task)
            {
                cancellationTokenSource.Cancel();
            }
            else
            {
                Assert.Fail("CreateCommentBatchAsync was not called within the expected time.");
            }
            await executeTask;

            // Assert
            _mockCommentService.Verify(s => s.CreateCommentBatchAsync(It.Is<List<Comment>>(list => list.Count == 5 && list.All(c => c.Text.StartsWith("Test comment")))), Times.Once);
        }
    }

    public class TestableRedisService : RedisToDbBackgroundService
    {
        public TestableRedisService(
            ILogger<RedisToDbBackgroundService> logger,
            IDatabase redisDatabase,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<BackgroundRedisOptions> settings)
            : base(logger, redisDatabase, serviceScopeFactory, settings)
        {
        }

        public new Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return base.ExecuteAsync(cancellationToken);
        }
    }
}